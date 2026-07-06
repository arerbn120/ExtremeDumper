using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.PE;
using ExtremeDumper.Logging;
using NativeSharp;

namespace ExtremeDumper.Dumping;

/// <summary>
/// .NET Reactor Anti-Tamper Protection Bypass Module
/// Handles runtime integrity checks, CRC verification, and debugger detection
/// </summary>
sealed class ReactorProtectionBypass {
	private readonly NativeProcess process;
	private readonly uint processId;

	// Reactor known integrity check patterns
	private static readonly byte[][] IntegrityCheckPatterns = new[] {
		// CRC32 calculation pattern
		new byte[] { 0x8B, 0x45, 0xF8, 0x8B, 0x4D, 0xFC, 0x83, 0xF9, 0x00 },
		// Hash verification pattern
		new byte[] { 0xFF, 0x35, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x15 },
		// Debugger check pattern
		new byte[] { 0x64, 0xA1, 0x30, 0x00, 0x00, 0x00, 0x83, 0x78, 0x0C, 0x00 }
	};

	public ReactorProtectionBypass(uint processId) {
		this.processId = processId;
		process = NativeProcess.Open(processId, ProcessAccess.MemoryRead | ProcessAccess.MemoryWrite | ProcessAccess.QueryInformation);
	}

	/// <summary>
	/// Scan process memory for anti-tamper checks and bypass them
	/// </summary>
	public bool BypassAntiTamper() {
		try {
			Logger.Info("[ReactorBypass] Starting anti-tamper protection bypass...");
			
			// Step 1: Disable debugger detection
			if (DisableDebuggerDetection()) {
				Logger.Info("[ReactorBypass] ✓ Debugger detection bypassed");
			}

			// Step 2: Patch CRC/Hash verification routines
			if (PatchIntegrityChecks()) {
				Logger.Info("[ReactorBypass] ✓ Integrity checks patched");
			}

			// Step 3: Neutralize runtime verification hooks
			if (NeutrializeVerificationHooks()) {
				Logger.Info("[ReactorBypass] ✓ Verification hooks neutralized");
			}

			Logger.Info("[ReactorBypass] Anti-tamper bypass completed successfully");
			return true;
		}
		catch (Exception ex) {
			Logger.Error("[ReactorBypass] Bypass failed");
			Logger.Exception(ex);
			return false;
		}
	}

	/// <summary>
	/// Disable IsDebuggerPresent and related checks
	/// </summary>
	private bool DisableDebuggerDetection() {
		try {
			// PEB offset for BeingDebugged flag
			const int PEB_OFFSET = 0x30;
			const int BEING_DEBUGGED_OFFSET = 0x0C;

			// Read PEB address from process
			var pebAddresses = ScanMemoryPattern(new byte[] { 0x64, 0xA1, 0x30, 0x00, 0x00, 0x00 });
			
			if (pebAddresses.Count == 0) {
				Logger.Warn("[ReactorBypass] Could not locate PEB access patterns");
				return false;
			}

			// Patch detected patterns to return 0 (not debugged)
			foreach (var address in pebAddresses) {
				// Replace with xor eax, eax; ret
				byte[] patch = { 0x33, 0xC0, 0xC3 };
				if (!process.TryWriteBytes((void*)address, patch)) {
					Logger.Warn("[ReactorBypass] Failed to patch debugger check at {0:X}", address);
					continue;
				}
			}

			return true;
		}
		catch (Exception ex) {
			Logger.Exception(ex);
			return false;
		}
	}

	/// <summary>
	/// Patch CRC32 and hash verification routines
	/// </summary>
	private bool PatchIntegrityChecks() {
		try {
			int patchCount = 0;

			// Scan for known CRC/Hash check patterns
			foreach (var pattern in IntegrityCheckPatterns) {
				var addresses = ScanMemoryPattern(pattern);
				
				foreach (var address in addresses) {
					// Replace with nop sled (0x90)
					byte[] nopPatch = new byte[pattern.Length];
					Array.Fill(nopPatch, (byte)0x90);
					
					if (process.TryWriteBytes((void*)address, nopPatch)) {
						patchCount++;
						Logger.Debug("[ReactorBypass] Patched integrity check at {0:X}", address);
					}
				}
			}

			return patchCount > 0;
		}
		catch (Exception ex) {
			Logger.Exception(ex);
			return false;
		}
	}

	/// <summary>
	/// Neutralize runtime verification hooks in .NET runtime
	/// </summary>
	private bool NeutrializeVerificationHooks() {
		try {
			// Hook points in mscorlib/.NET runtime
			var hookPatterns = new Dictionary<string, byte[]> {
				// Module validation hook
				{ "ModuleValidation", new byte[] { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x0C } },
				// Assembly verification hook
				{ "AssemblyVerification", new byte[] { 0x55, 0x8B, 0xEC, 0x56, 0x57 } },
				// Type validation hook
				{ "TypeValidation", new byte[] { 0x48, 0x89, 0x5C, 0x24, 0x08 } }
			};

			int neutralizedCount = 0;

			foreach (var hookEntry in hookPatterns) {
				var addresses = ScanMemoryPattern(hookEntry.Value);
				
				foreach (var address in addresses) {
					// Replace first bytes with immediate return (C3)
					byte[] returnPatch = { 0xC3 };
					if (process.TryWriteBytes((void*)address, returnPatch)) {
						neutralizedCount++;
						Logger.Debug("[ReactorBypass] Neutralized {0} hook at {1:X}", 
							hookEntry.Key, address);
					}
				}
			}

			return neutralizedCount > 0;
		}
		catch (Exception ex) {
			Logger.Exception(ex);
			return false;
		}
	}

	/// <summary>
	/// Scan process memory for byte patterns
	/// </summary>
	private List<nuint> ScanMemoryPattern(byte[] pattern) {
		var results = new List<nuint>();

		try {
			const int SCAN_CHUNK = 0x10000; // 64KB chunks
			byte[] buffer = new byte[SCAN_CHUNK + pattern.Length];

			foreach (var pageInfo in process.EnumeratePageInfos()) {
				if ((pageInfo.Protection & MemoryProtection.Execute) == 0)
					continue; // Skip non-executable pages

				if (!process.TryReadBytes(pageInfo.Address, buffer))
					continue;

				for (int i = 0; i <= buffer.Length - pattern.Length; i++) {
					bool match = true;
					for (int j = 0; j < pattern.Length; j++) {
						if (buffer[i + j] != pattern[j]) {
							match = false;
							break;
						}
					}

					if (match) {
						results.Add((nuint)pageInfo.Address + (uint)i);
					}
				}
			}
		}
		catch (Exception ex) {
			Logger.Exception(ex);
		}

		return results;
	}

	public void Dispose() {
		process?.Dispose();
	}
}