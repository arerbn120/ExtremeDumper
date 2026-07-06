using System;
using System.IO;
using dnlib.DotNet;
using dnlib.PE;
using ExtremeDumper.AntiAntiDump;
using ExtremeDumper.Logging;

namespace ExtremeDumper.Dumping;

/// <summary>
/// Advanced anti-anti-dump with Reactor protection bypass and nested VM handling
/// </summary>
sealed class AdvancedAntiAntiDumper : AntiAntiDumper {
	private ReactorProtectionBypass? protectionBypass;
	private NestedVMHandler? vmHandler;

	public AdvancedAntiAntiDumper(uint processId) : base(processId) {
	}

	public override bool DumpModule(nuint moduleHandle, ImageLayout imageLayout, string filePath) {
		ReactorProtectionBypass? bypass = null;
		
		try {
			// Step 1: Initialize protection bypass
			bypass = new ReactorProtectionBypass(process.Id);
			Logger.Info("[AdvancedDump] Initializing Reactor protection bypass");

			if (bypass.BypassAntiTamper()) {
				Logger.Info("[AdvancedDump] ✓ Anti-tamper protection bypassed");
			} else {
				Logger.Warn("[AdvancedDump] Anti-tamper bypass attempt completed (may not have found checks)");
			}

			// Step 2: Perform standard dump
			Logger.Info("[AdvancedDump] Dumping module...");
			if (!base.DumpModule(moduleHandle, imageLayout, filePath)) {
				Logger.Error("[AdvancedDump] Standard dump failed");
				return false;
			}

			// Step 3: Analyze and handle nested VM
			Logger.Info("[AdvancedDump] Analyzing for nested VM virtualization...");
			try {
				using var dumpedData = File.ReadAllBytes(filePath);
				using var dumpedImage = new PEImage(dumpedData);
				using var dumpedModule = ModuleDefMD.Load(dumpedImage);

				var vmHandler = new NestedVMHandler(dumpedModule, dumpedImage);
				if (vmHandler.AnalyzeAndExtractNestedVM(out var recoveredMethods)) {
					Logger.Info("[AdvancedDump] ✓ Recovered {0} virtualized methods", recoveredMethods.Count);

					// Add recovered methods to module
					foreach (var recoveredMethod in recoveredMethods) {
						try {
							var targetType = dumpedModule.ResolveToken(recoveredMethod.MDToken) as TypeDef;
							if (targetType != null) {
								// Replace virtualized method with recovered version
								var existingMethod = targetType.Methods.FirstOrDefault(m => m.Name == recoveredMethod.Name);
								if (existingMethod != null) {
									existingMethod.Body = recoveredMethod.Body;
									Logger.Debug("[AdvancedDump] Updated method: {0}.{1}", targetType.Name, recoveredMethod.Name);
								}
							}
						}
						catch (Exception ex) {
							Logger.Debug("[AdvancedDump] Could not integrate recovered method: {0}", ex.Message);
						}
					}

					// Save updated module
					dumpedModule.Write(filePath);
					Logger.Info("[AdvancedDump] Saved updated module with recovered methods");
				}
			}
			catch (Exception ex) {
				Logger.Warn("[AdvancedDump] Nested VM analysis failed (dump still valid): {0}", ex.Message);
				// Don't fail the entire dump if VM analysis fails
			}

			Logger.Info("[AdvancedDump] ✓ Advanced dump completed successfully");
			return true;
		}
		catch (Exception ex) {
			Logger.Error("[AdvancedDump] Advanced dump failed");
			Logger.Exception(ex);
			return false;
		}
		finally {
			bypass?.Dispose();
		}
	}

	public override int DumpProcess(string directoryPath) {
		throw new NotSupportedException("Use base AntiAntiDumper for process-wide dumps");
	}
}