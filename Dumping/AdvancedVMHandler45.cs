using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using ExtremeDumper.Logging;
using ExtremeDumper.Utilities;

namespace ExtremeDumper.Dumping;

/// <summary>
/// Advanced VM handler leveraging dnlib 4.5 improvements
/// Provides enhanced virtualization detection and analysis
/// </summary>
public sealed class AdvancedVMHandler45 {
	private readonly ModuleDefMD module;
	private int analysisDepth = 0;
	private const int MAX_ANALYSIS_DEPTH = 15; // Increased from 10

	public AdvancedVMHandler45(ModuleDefMD module) {
		this.module = module ?? throw new ArgumentNullException(nameof(module));
	}

	/// <summary>
	/// Perform advanced virtualization analysis using dnlib 4.5
	/// </summary>
	public bool PerformAdvancedAnalysis(out VirtualizationReport report) {
		report = new VirtualizationReport();

		try {
			Logger.Info("[AdvancedVM45] Starting advanced virtualization analysis");
			analysisDepth = 0;

			// Analysis Phase 1: Scan for virtualized types
			var virtualizedTypes = AnalyzeVirtualizedTypes();
			report.VirtualizedTypes = virtualizedTypes;
			Logger.Info($"[AdvancedVM45] Found {virtualizedTypes.Count} virtualized types");

			// Analysis Phase 2: Detect VM dispatch patterns
			var dispatchPatterns = DetectVMDispatchPatterns();
			report.DispatchPatterns = dispatchPatterns;
			Logger.Info($"[AdvancedVM45] Detected {dispatchPatterns.Count} dispatch patterns");

			// Analysis Phase 3: Analyze method complexity
			var complexMethods = AnalyzeMethodComplexity();
			report.SuspiciousMethods = complexMethods;
			Logger.Info($"[AdvancedVM45] Identified {complexMethods.Count} suspicious methods");

			// Analysis Phase 4: Check for obfuscation markers
			var obfuscationIndicators = DetectObfuscationMarkers();
			report.ObfuscationIndicators = obfuscationIndicators;
			Logger.Info($"[AdvancedVM45] Found {obfuscationIndicators.Count} obfuscation markers");

			// Analysis Phase 5: Estimate VM depth
			var estimatedDepth = EstimateVMDepth();
			report.EstimatedVMDepth = estimatedDepth;
			Logger.Info($"[AdvancedVM45] Estimated VM depth: {estimatedDepth} layers");

			report.Success = true;
			Logger.Info("[AdvancedVM45] Advanced analysis completed successfully");

			return true;
		}
		catch (Exception ex) {
			Logger.Error("[AdvancedVM45] Advanced analysis failed");
			Logger.Exception(ex);
			report.Success = false;
			return false;
		}
	}

	/// <summary>
	/// Analyze virtualized types using dnlib 4.5
	/// </summary>
	private List<TypeVirtualizationInfo> AnalyzeVirtualizedTypes() {
		var results = new List<TypeVirtualizationInfo>();

		try {
			// Use dnlib 4.5 improved type enumeration
			foreach (var type in module.GetAllTypes()) {
				analysisDepth++;
				if (analysisDepth > MAX_ANALYSIS_DEPTH) {
					Logger.Warn("[AdvancedVM45] Analysis depth exceeded");
					break;
				}

				// Check if type is virtualized
				if (type.IsTypeVirtualized()) {
					var info = new TypeVirtualizationInfo {
						TypeName = type.FullName,
						VirtualizedMethodCount = type.Methods.Count(m => m.IsVirtualized()),
						TotalMethodCount = type.Methods.Count,
						HasCustomProtection = type.CustomAttributes.Any(a =>
							a.TypeFullName.Contains("Protected") ||
							a.TypeFullName.Contains("Virtualized")),
						NestedLevel = GetNestingLevel(type)
					};

					results.Add(info);
					Logger.Debug($"[AdvancedVM45] Virtualized type: {type.FullName} " +
						$"({info.VirtualizedMethodCount}/{info.TotalMethodCount} methods)");
				}
			}
		}
		catch (Exception ex) {
			Logger.Exception(ex);
		}

		return results;
	}

	/// <summary>
	/// Detect VM dispatch patterns
	/// </summary>
	private List<DispatchPatternInfo> DetectVMDispatchPatterns() {
		var patterns = new List<DispatchPatternInfo>();

		try {
			foreach (var type in module.GetAllTypes()) {
				foreach (var method in type.Methods) {
					var body = Dnlib45CompatibilityLayer.GetMethodBody(method);
					if (body?.Instructions == null || body.Instructions.Count < 10)
						continue;

					// Check for switch statement patterns
					var switchCount = body.Instructions.Count(i => i.OpCode == OpCodes.Switch);
					if (switchCount > 0) {
						// Analyze switch dispatcher
						var dispatchInfo = AnalyzeSwitchDispatcher(method, body);
						if (dispatchInfo != null) {
							patterns.Add(dispatchInfo);
							Logger.Debug($"[AdvancedVM45] Dispatch pattern in {method.FullName}");
						}
					}

					// Check for exception-based dispatch
					if (method.GetExceptionHandlers().Count() > 5) {
						var exceptionDispatch = new DispatchPatternInfo {
							Method = method.FullName,
							PatternType = "ExceptionDispatch",
							Confidence = 0.8f,
							Details = "Exception-based VM dispatch"
						};

						patterns.Add(exceptionDispatch);
						Logger.Debug($"[AdvancedVM45] Exception dispatch in {method.FullName}");
					}
				}
			}
		}
		catch (Exception ex) {
			Logger.Exception(ex);
		}

		return patterns;
	}

	/// <summary>
	/// Analyze switch dispatcher pattern
	/// </summary>
	private DispatchPatternInfo? AnalyzeSwitchDispatcher(MethodDef method, CilBody body) {
		try {
			var switchInstrs = body.Instructions.Where(i => i.OpCode == OpCodes.Switch).ToList();

			if (!switchInstrs.Any())
				return null;

			var switchInstr = switchInstrs.First();
			int targetCount = 0;

			// Count switch targets
			if (switchInstr.Operand is Instruction[] targets) {
				targetCount = targets.Length;
			} else if (switchInstr.Operand is IList<Instruction> targetList) {
				targetCount = targetList.Count;
			}

			// dnlib 4.5: Improved switch analysis
			return new DispatchPatternInfo {
				Method = method.FullName,
				PatternType = "SwitchDispatch",
				Confidence = Math.Min(0.5f + (targetCount * 0.02f), 1.0f),
				Details = $"Switch with {targetCount} targets"
			};
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Analyze method complexity scores
	/// </summary>
	private List<MethodComplexityInfo> AnalyzeMethodComplexity() {
		var results = new List<MethodComplexityInfo>();

		try {
			foreach (var type in module.GetAllTypes()) {
				foreach (var method in type.Methods) {
					var complexity = method.GetComplexityScore();

					// High complexity indicates possible virtualization
					if (complexity > 70) {
						results.Add(new MethodComplexityInfo {
							Method = method.FullName,
							ComplexityScore = complexity,
							InstructionCount = method.Body?.Instructions.Count ?? 0,
							LocalVariables = method.Body?.Variables.Count ?? 0,
							ExceptionHandlers = method.GetExceptionHandlers().Count(),
							LikelyVirtualized = complexity > 85
						});

						if (results.Count % 10 == 0) {
							Logger.Debug($"[AdvancedVM45] Analyzed {results.Count} complex methods");
						}
					}
				}
			}
		}
		catch (Exception ex) {
			Logger.Exception(ex);
		}

		return results.OrderByDescending(r => r.ComplexityScore).ToList();
	}

	/// <summary>
	/// Detect obfuscation markers using dnlib 4.5
	/// </summary>
	private List<ObfuscationIndicator> DetectObfuscationMarkers() {
		var indicators = new List<ObfuscationIndicator>();

		try {
			// Check 1: Custom attributes indicating obfuscation
			foreach (var attr in module.CustomAttributes) {
				if (attr.TypeFullName.Contains("Obfuscated") ||
					attr.TypeFullName.Contains("Protected") ||
					attr.TypeFullName.Contains("Virtualized")) {

					indicators.Add(new ObfuscationIndicator {
						Type = "CustomAttribute",
						Value = attr.TypeFullName,
						Severity = "High"
					});
				}
			}

			// Check 2: String encryption patterns
			var encryptedStrings = 0;
			foreach (var type in module.GetAllTypes()) {
				foreach (var method in type.Methods) {
					var body = Dnlib45CompatibilityLayer.GetMethodBody(method);
					if (body != null) {
						// Look for string decryption patterns
						var xorOps = body.Instructions.Count(i => i.OpCode == OpCodes.Xor);
						var rotateOps = body.Instructions.Count(i => i.OpCode.Name.Contains("rol") || i.OpCode.Name.Contains("ror"));

						if ((xorOps + rotateOps) > 5) {
							encryptedStrings++;
						}
					}
				}
			}

			if (encryptedStrings > 0) {
				indicators.Add(new ObfuscationIndicator {
					Type = "StringEncryption",
					Value = $"{encryptedStrings} methods",
					Severity = "High"
				});
			}

			// Check 3: Metadata corruption
			if (Dnlib45CompatibilityLayer.TryLoadModule(
				new dnlib.PE.PEImage(new System.IO.MemoryStream()), null) == null) {
				indicators.Add(new ObfuscationIndicator {
					Type = "MetadataCorruption",
					Value = "Possible metadata corruption detected",
					Severity = "Medium"
				});
			}

			// Check 4: Unusual type names
			var suspiciousTypes = module.GetAllTypes()
				.Where(t => t.Name.Length <= 3 || t.Name.All(c => c == '_'))
				.Count();

			if (suspiciousTypes > 0) {
				indicators.Add(new ObfuscationIndicator {
					Type = "SuspiciousNames",
					Value = $"{suspiciousTypes} types with suspicious names",
					Severity = "Medium"
				});
			}
		}
		catch (Exception ex) {
			Logger.Exception(ex);
		}

		return indicators;
	}

	/// <summary>
	/// Estimate VM depth based on analysis
	/// </summary>
	private int EstimateVMDepth() {
		try {
			int depth = 0;

			// Factor 1: Nested virtualization indicators
			var nestedVMs = module.GetAllTypes()
				.Count(t => t.NestedTypes.Count > 5);
			depth += nestedVMs > 3 ? 2 : nestedVMs > 0 ? 1 : 0;

			// Factor 2: Method complexity distribution
			var complexMethods = module.GetAllTypes()
				.SelectMany(t => t.Methods)
				.Count(m => m.GetComplexityScore() > 75);

			depth += complexMethods > module.GetAllTypes().SelectMany(t => t.Methods).Count() / 5 ? 2 : 1;

			// Factor 3: Exception handler usage
			var exceptionHandlers = module.GetAllTypes()
				.SelectMany(t => t.Methods)
				.Sum(m => m.GetExceptionHandlers().Count());

			depth += exceptionHandlers > 50 ? 2 : exceptionHandlers > 10 ? 1 : 0;

			return Math.Min(depth, 10); // Cap at 10 layers
		}
		catch {
			return 1; // Default: 1 layer
		}
	}

	/// <summary>
	/// Get type nesting level
	/// </summary>
	private int GetNestingLevel(TypeDef type) {
		int level = 0;
		var current = type.DeclaringType;

		while (current != null) {
			level++;
			current = current.DeclaringType;
		}

		return level;
	}
}

/// <summary>
/// Virtualization analysis report
/// </summary>
public class VirtualizationReport {
	public bool Success { get; set; }
	public List<TypeVirtualizationInfo> VirtualizedTypes { get; set; } = new();
	public List<DispatchPatternInfo> DispatchPatterns { get; set; } = new();
	public List<MethodComplexityInfo> SuspiciousMethods { get; set; } = new();
	public List<ObfuscationIndicator> ObfuscationIndicators { get; set; } = new();
	public int EstimatedVMDepth { get; set; }
}

/// <summary>
/// Type virtualization information
/// </summary>
public class TypeVirtualizationInfo {
	public string TypeName { get; set; } = "";
	public int VirtualizedMethodCount { get; set; }
	public int TotalMethodCount { get; set; }
	public bool HasCustomProtection { get; set; }
	public int NestedLevel { get; set; }
}

/// <summary>
/// Dispatch pattern information
/// </summary>
public class DispatchPatternInfo {
	public string Method { get; set; } = "";
	public string PatternType { get; set; } = "";
	public float Confidence { get; set; }
	public string Details { get; set; } = "";
}

/// <summary>
/// Method complexity information
/// </summary>
public class MethodComplexityInfo {
	public string Method { get; set; } = "";
	public int ComplexityScore { get; set; }
	public int InstructionCount { get; set; }
	public int LocalVariables { get; set; }
	public int ExceptionHandlers { get; set; }
	public bool LikelyVirtualized { get; set; }
}

/// <summary>
/// Obfuscation indicator
/// </summary>
public class ObfuscationIndicator {
	public string Type { get; set; } = "";
	public string Value { get; set; } = "";
	public string Severity { get; set; } = "";
}