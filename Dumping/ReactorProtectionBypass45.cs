using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ExtremeDumper.Logging;
using NativeSharp;

namespace ExtremeDumper.Dumping;

/// <summary>
/// Enhanced .NET Reactor Anti-Tamper Protection Bypass for dnlib 4.5
/// Improved pattern detection and compatibility
/// </summary>
sealed class ReactorProtectionBypass45 {
	private readonly NativeProcess process;
	private readonly uint processId;
	private int patchCount = 0;

	// Enhanced Reactor protection patterns for 4.5
	private static readonly byte[][] IntegrityCheckPatterns = new[] {
		// CRC32 calculation pattern
		new byte[] { 0x8B, 0x45, 0xF8, 0x8B, 0x4D, 0xFC, 0x83, 0xF9, 0x00 },
		// Hash verification pattern
		new byte[] { 0xFF, 0x35, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x15 },
		// Debugger check pattern
		new byte[] { 0x64, 0xA1, 0x30, 0x00, 0x00, 0x00, 0x83, 0x78, 0x0C, 0x00 },
		// Reactor 5.x CRC pattern
		new byte[] { 0x48, 0x8B, 0x45, 0xF8, 0x48, 0x8B, 0x4D, 0xF0 },
		// Enhanced string validation
		new byte[] { 0x83, 0xE8, 0x01, 0x83, 0xF8, 0x00, 0x0F, 0x84 }
	};

	// Additional protection markers
	private static readonly byte[][] ProtectionMarkers = new[] {
		// Anti-dump marker
		new byte[] { 0x65, 0x48, 0x8B, 0x04, 0x25, 0x60, 0x00, 0x00, 0x00 },
		// Anti-debug marker
		new byte[] { 0x65, 0x64, 0x8B, 0x04, 0x25, 0x30, 0x00, 0x00, 0x00 },
		// Anti-VM marker
		new byte[] { 0xFF, 0x15, 0x00, 0x00, 0x00, 0x00, 0x85, 0xC0 }
	};

	public ReactorProtectionBypass45(uint processId) {
		this.processId = processId;
		process = NativeProcess.Open(processId, 
			ProcessAccess.MemoryRead | 
			ProcessAccess.MemoryWrite | 
			ProcessAccess.QueryInformation);
	}

	/// <summary>
	/// Perform comprehensive anti-tamper bypass with dnlib 4.5 awareness
	/// </summary>
	public bool BypassAntiTamper() {
		try {
			Logger.Info("[ReactorBypass45] Starting anti-tamper protection bypass v4.5");
			patchCount = 0;

			// Step 1: Disable debugger detection
			if (DisableDebuggerDetection45()) {
				Logger.Info("[ReactorBypass45] ✓ Debugger detection bypassed");
			}

			// Step 2: Patch CRC/Hash verification routines
			if (PatchIntegrityChecks45()) {
				Logger.Info("[ReactorBypass45] ✓ Integrity checks patched");
			}

			// Step 3: Neutralize verification hooks
			if (NeutrializeVerificationHooks45()) {
				Logger.Info("[ReactorBypass45] ✓ Verification hooks neutralized");
			}

			// Step 4: Disable anti-VM checks
			if (DisableAntiVMChecks45()) {
				Logger.Info("[ReactorBypass45] ✓ Anti-VM checks disabled");
			}

			// Step 5: Patch protection markers
			if (PatchProtectionMarkers45()) {
				Logger.Info("[ReactorBypass45] ✓ Protection markers patched");
			}

			Logger.Info($"[ReactorBypass45] Anti-tamper bypass completed (${patchCount} patches applied)");
			return patchCount > 0;
		}
		catch (Exception ex) {
			Logger.Error("[ReactorBypass45] Bypass failed");
			Logger.Exception(ex);
			return false;
		}
	}

	/// <summary>
	/// Disable IsDebuggerPresent and related checks (enhanced for 4.5)
	/// </summary>
	private bool DisableDebuggerDetection45() {
		try {
			const int PEB_OFFSET = 0x30;
			const int BEING_DEBUGGED_OFFSET = 0x0C;
			const int NTGlobal_FLAGS_OFFSET = 0x68;

			var debuggerPatterns = new[] {
				new byte[] { 0x64, 0xA1, 0x30, 0x00, 0x00, 0x00 }, // x86
				new byte[] { 0x65, 0x48, 0x8B, 0x04, 0x25, 0x60, 0x00, 0x00, 0x00 } // x64
			};

			int patchedCount = 0;

			foreach (var pattern in debuggerPatterns) {
				var addresses = ScanMemoryPattern45(pattern);
				
				foreach (var address in addresses) {
					// Replace with xor eax, eax; ret
					byte[] patch = { 0x33, 0xC0, 0xC3 };
					if (process.TryWriteBytes((void*)address, patch)) {
						patchedCount++;
						patchCount++;
						Logger.Debug($"[ReactorBypass45] Patched debugger check at {address:X}");
					}
				}
			}

			return patchedCount > 0;
		}
		catch (Exception ex) {
			Logger.Exception(ex);
			return false;
		}
	}

	/// <summary>
	/// Patch CRC32 and hash verification with enhanced Reactor 5.x support
	/// </summary>
	private bool PatchIntegrityChecks45() {
		try {
			int patchedCount = 0;

			foreach (var pattern in IntegrityCheckPatterns) {
				var addresses = ScanMemoryPattern45(pattern);
				
				foreach (var address in addresses) {
					// Create appropriate patch based on pattern
					byte[] patch = GenerateNopPatch(pattern.Length);
					
					if (process.TryWriteBytes((void*)address, patch)) {
						patchedCount++;
						patchCount++;
						Logger.Debug($"[ReactorBypass45] Patched integrity check at {address:X}");
					}
				}
			}

			return patchedCount > 0;
		}
		catch (Exception ex) {
			Logger.Exception(ex);
			return false;
		}
	}

	/// <summary>
	/// Neutralize runtime verification hooks
	/// </summary>
	private bool NeutrializeVerificationHooks45() {
		try {
			var hookPatterns = new Dictionary<string, byte[]> {
				// Module validation hook
				{ "ModuleValidation", new byte[] { 0x55, 0x8B, 0xEC, 0x83, 0xEC, 0x0C } },
				// Assembly verification hook
				{ "AssemblyVerification", new byte[] { 0x55, 0x8B, 0xEC, 0x56, 0x57 } },
				// Type validation hook
				{ "TypeValidation", new byte[] { 0x48, 0x89, 0x5C, 0x24, 0x08 } },
				// Reactor 5.x hook pattern
				{ "ReactorHook", new byte[] { 0xFF, 0x25, 0x00, 0x00, 0x00, 0x00 } }
			};

			int neutralizedCount = 0;

			foreach (var hookEntry in hookPatterns) {
				var addresses = ScanMemoryPattern45(hookEntry.Value);
				
				foreach (var address in addresses) {
					byte[] returnPatch = { 0xC3 };
					if (process.TryWriteBytes((void*)address, returnPatch)) {
						neutralizedCount++;
						patchCount++;
						Logger.Debug($"[ReactorBypass45] Neutralized {hookEntry.Key} at {address:X}");
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
	/// Disable anti-VM detection checks
	/// </summary>
	private bool DisableAntiVMChecks45() {
		try {
			int disabledCount = 0;

			// Check for VM detection patterns
			var vmDetectionPatterns = new[] {
				// CPUID-based VM detection
				new byte[] { 0x0F, 0xA2, 0x83, 0xF8, 0x00 },
				// MSR-based VM detection
				new byte[] { 0x0F, 0x32, 0x89, 0x45, 0xF8 },
				// Exception-based VM detection
				new byte[] { 0x55, 0x8B, 0xEC, 0x64, 0xFF }
			};

			foreach (var pattern in vmDetectionPatterns) {
				var addresses = ScanMemoryPattern45(pattern);
				
				foreach (var address in addresses) {
					byte[] patch = GenerateNopPatch(pattern.Length);
					if (process.TryWriteBytes((void*)address, patch)) {
						disabledCount++;
						patchCount++;
						Logger.Debug($"[ReactorBypass45] Disabled VM check at {address:X}");
					}
				}
			}

			return disabledCount > 0;
		}
		catch (Exception ex) {
			Logger.Exception(ex);
			return false;
		}
	}

	/// <summary>
	/// Patch protection markers
	/// </summary>
	private bool PatchProtectionMarkers45() {
		try {
			int patchedCount = 0;

			foreach (var marker in ProtectionMarkers) {
				var addresses = ScanMemoryPattern45(marker);
				
				foreach (var address in addresses) {
					byte[] patch = GenerateNopPatch(marker.Length);
					if (process.TryWriteBytes((void*)address, patch)) {
						patchedCount++;
						patchCount++;
						Logger.Debug($"[ReactorBypass45] Patched protection marker at {address:X}");
					}
				}
			}

			return patchedCount > 0;
		}
		catch (Exception ex) {
			Logger.Exception(ex);
			return false;
		}
	}

	/// <summary>
	/// Enhanced memory pattern scanning with optimization
	/// </summary>
	private List<nuint> ScanMemoryPattern45(byte[] pattern) {
		var results = new List<nuint>();

		try {
			const int SCAN_CHUNK = 0x10000;
			byte[] buffer = new byte[SCAN_CHUNK + pattern.Length];

			foreach (var pageInfo in process.EnumeratePageInfos()) {
				if ((pageInfo.Protection & MemoryProtection.Execute) == 0)
					continue;

				if (!process.TryReadBytes(pageInfo.Address, buffer))
					continue;

				for (int i = 0; i <= buffer.Length - pattern.Length; i++) {
					if (MatchPattern(buffer, i, pattern)) {
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

	/// <summary>
	/// Optimized pattern matching
	/// </summary>
	private bool MatchPattern(byte[] buffer, int offset, byte[] pattern) {
		if (offset + pattern.Length > buffer.Length)
			return false;

		for (int i = 0; i < pattern.Length; i++) {
			if (buffer[offset + i] != pattern[i])
				return false;
		}

		return true;
	}

	/// <summary>
	/// Generate NOP sled for patching
	/// </summary>
	private byte[] GenerateNopPatch(int length) {
		byte[] patch = new byte[length];
		for (int i = 0; i < length; i++) {
			patch[i] = 0x90; // NOP opcode
		}
		return patch;
	}

	public void Dispose() {
		process?.Dispose();
	}
}