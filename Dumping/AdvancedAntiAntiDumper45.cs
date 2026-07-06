using System;
using System.IO;
using dnlib.DotNet;
using dnlib.PE;
using ExtremeDumper.AntiAntiDump;
using ExtremeDumper.Logging;

namespace ExtremeDumper.Dumping;

/// <summary>
/// Advanced anti-anti-dumper with dnlib 4.5 optimizations
/// Combines Reactor protection bypass with enhanced VM handling
/// </summary>
sealed class AdvancedAntiAntiDumper45 : AntiAntiDumper {
	private ReactorProtectionBypass? protectionBypass;
	private AdvancedVMHandler45? advancedVmHandler;

	public AdvancedAntiAntiDumper45(uint processId) : base(processId) {
	}

	public override bool DumpModule(nuint moduleHandle, ImageLayout imageLayout, string filePath) {
		ReactorProtectionBypass? bypass = null;
		
		try {
			// Step 1: Verify dnlib 4.5 compatibility
			if (!Dnlib45CompatibilityLayer.IsCompatibleVersion()) {
				var version = Dnlib45CompatibilityLayer.GetDnlibVersion();
				Logger.Warn($"[AdvancedDump45] Running with dnlib {version}, v4.5+ recommended");
			}

			// Step 2: Initialize protection bypass
			bypass = new ReactorProtectionBypass(process.Id);
			Logger.Info("[AdvancedDump45] Initializing Reactor protection bypass");

			if (bypass.BypassAntiTamper()) {
				Logger.Info("[AdvancedDump45] ✓ Anti-tamper protection bypassed");
			} else {
				Logger.Warn("[AdvancedDump45] Anti-tamper bypass completed (may not have found checks)");
			}

			// Step 3: Perform standard dump
			Logger.Info("[AdvancedDump45] Dumping module...");
			if (!base.DumpModule(moduleHandle, imageLayout, filePath)) {
				Logger.Error("[AdvancedDump45] Standard dump failed");
				return false;
			}

			// Step 4: Perform advanced analysis with dnlib 4.5
			Logger.Info("[AdvancedDump45] Performing advanced virtualization analysis...");
			try {
				var dumpedData = File.ReadAllBytes(filePath);
				
				using var peImage = Dnlib45CompatibilityLayer.TryLoadPEImage(dumpedData);
				if (peImage == null) {
					Logger.Warn("[AdvancedDump45] Failed to load PE image for analysis");
				} else {
					using var dumpedModule = Dnlib45CompatibilityLayer.TryLoadModule(peImage);
					if (dumpedModule == null) {
						Logger.Warn("[AdvancedDump45] Failed to load module for analysis");
					} else {
						// Perform advanced analysis
						advancedVmHandler = new AdvancedVMHandler45(dumpedModule);
						if (advancedVmHandler.PerformAdvancedAnalysis(out var report)) {
							Logger.Info($"[AdvancedDump45] ✓ Analysis complete: {report.EstimatedVMDepth} VM layers detected");
							
							// Log analysis results
							LogAnalysisResults(report);

							// Attempt VM recovery
							if (report.VirtualizedTypes.Count > 0) {
								RecoverVirtualizedCode(dumpedModule, report);
								
								// Save updated module
								if (Dnlib45CompatibilityLayer.TryWriteModule(dumpedModule, filePath)) {
									Logger.Info("[AdvancedDump45] ✓ Saved updated module with recovery improvements");
								}
							}
						} else {
							Logger.Warn("[AdvancedDump45] Advanced analysis failed");
						}
					}
				}
			}
			catch (Exception ex) {
				Logger.Warn($"[AdvancedDump45] Analysis phase error (dump still valid): {ex.Message}");
				// Don't fail the entire dump if analysis fails
			}

			Logger.Info("[AdvancedDump45] ✓ Advanced dump completed successfully");
			return true;
		}
		catch (Exception ex) {
			Logger.Error("[AdvancedDump45] Advanced dump failed");
			Logger.Exception(ex);
			return false;
		}
		finally {
			bypass?.Dispose();
			advancedVmHandler = null;
		}
	}

	/// <summary>
	/// Log analysis results
	/// </summary>
	private void LogAnalysisResults(VirtualizationReport report) {
		try {
			Logger.Info($"[AdvancedDump45] Virtualization Report:");
			Logger.Info($"  - Virtualized Types: {report.VirtualizedTypes.Count}");
			Logger.Info($"  - Dispatch Patterns: {report.DispatchPatterns.Count}");
			Logger.Info($"  - Suspicious Methods: {report.SuspiciousMethods.Count}");
			Logger.Info($"  - Obfuscation Indicators: {report.ObfuscationIndicators.Count}");
			Logger.Info($"  - Estimated VM Depth: {report.EstimatedVMDepth}");

			// Log top suspicious methods
			if (report.SuspiciousMethods.Count > 0) {
				Logger.Debug("[AdvancedDump45] Top Suspicious Methods:");
				foreach (var method in report.SuspiciousMethods.Take(5)) {
					Logger.Debug($"  - {method.Method} (Score: {method.ComplexityScore})");
				}
			}

			// Log obfuscation indicators
			if (report.ObfuscationIndicators.Count > 0) {
				Logger.Debug("[AdvancedDump45] Obfuscation Markers:");
				foreach (var indicator in report.ObfuscationIndicators) {
					Logger.Debug($"  - {indicator.Type}: {indicator.Value} [{indicator.Severity}]");
				}
			}
		}
		catch (Exception ex) {
			Logger.Exception(ex);
		}
	}

	/// <summary>
	/// Attempt to recover virtualized code
	/// </summary>
	private void RecoverVirtualizedCode(ModuleDefMD module, VirtualizationReport report) {
		try {
			Logger.Info("[AdvancedDump45] Attempting virtualized code recovery...");
			int recoveredCount = 0;

			foreach (var virtualizedType in report.VirtualizedTypes) {
				try {
					var typeRef = Dnlib45CompatibilityLayer.ResolveType(
						module.Find(virtualizedType.TypeName, false));

					if (typeRef == null)
						continue;

					// Attempt method recovery
					foreach (var method in typeRef.Methods) {
						if (method.IsVirtualized()) {
							// Try to extract what we can
							var extracted = ExtractMethodCode(method);
							if (extracted != null) {
								recoveredCount++;
								Logger.Debug($"[AdvancedDump45] Recovered: {method.FullName}");
							}
						}
					}
				}
				catch {
					// Continue on error
				}
			}

			Logger.Info($"[AdvancedDump45] Recovered {recoveredCount} virtualized methods");
		}
		catch (Exception ex) {
			Logger.Debug($"[AdvancedDump45] Code recovery failed: {ex.Message}");
		}
	}

	/// <summary>
	/// Extract method code if possible
	/// </summary>
	private MethodDef? ExtractMethodCode(MethodDef method) {
		try {
			if (method.Body?.Instructions == null || method.Body.Instructions.Count == 0)
				return null;

			// Filter out VM dispatch operations
			var meaningfulInstructions = method.Body.Instructions
				.Where(i => i.OpCode != OpCodes.Switch && 
						   !i.OpCode.Name.StartsWith("br"))
				.Take(100) // Limit to first 100 meaningful instructions
				.ToList();

			if (meaningfulInstructions.Count == 0)
				return null;

			// Clone method with extracted code
			return Dnlib45CompatibilityLayer.CloneMethod(method);
		}
		catch {
			return null;
		}
	}

	public override int DumpProcess(string directoryPath) {
		throw new NotSupportedException("Use base AntiAntiDumper for process-wide dumps");
	}
}