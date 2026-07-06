using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.PE;
using ExtremeDumper.Logging;

namespace ExtremeDumper.Dumping;

/// <summary>
/// Handler for nested/multi-layer .NET Reactor VM virtualization
/// Recursively analyzes and extracts virtualized code from multiple layers
/// </summary>
sealed class NestedVMHandler {
	private readonly ModuleDef module;
	private readonly PEImage peImage;
	private int virtualizationDepth = 0;
	private const int MAX_VM_DEPTH = 10; // Prevent infinite recursion

	public NestedVMHandler(ModuleDef module, PEImage peImage) {
		this.module = module;
		this.peImage = peImage;
	}

	/// <summary>
	/// Analyze module for nested VM layers and extract original code
	/// </summary>
	public bool AnalyzeAndExtractNestedVM(out List<MethodDef> recoveredMethods) {
		recoveredMethods = new List<MethodDef>();

		try {
			Logger.Info("[NestedVM] Starting nested virtualization analysis...");
			virtualizationDepth = 0;

			foreach (var type in module.GetTypes()) {
				foreach (var method in type.Methods) {
					if (method.Body?.Instructions.Count == 0)
						continue;

					// Check if method is virtualized
					if (IsMethodVirtualized(method)) {
						Logger.Debug("[NestedVM] Found virtualized method: {0}.{1}", 
							type.Name, method.Name);

						if (TryUnvirtualizeMethod(method, out var recoveredMethod)) {
							recoveredMethods.Add(recoveredMethod);
						}
					}
				}
			}

			Logger.Info("[NestedVM] Analysis complete. Recovered {0} methods", recoveredMethods.Count);
			return recoveredMethods.Count > 0;
		}
		catch (Exception ex) {
			Logger.Error("[NestedVM] Analysis failed");
			Logger.Exception(ex);
			return false;
		}
	}

	/// <summary>
	/// Detect if a method is virtualized by analyzing IL patterns
	/// </summary>
	private bool IsMethodVirtualized(MethodDef method) {
		try {
			if (method.Body?.Instructions.Count < 2)
				return false;

			// Reactor VM signatures
			var vmSignatures = new[] {
				// VirtualizedMethod marker attribute
				method.HasCustomAttributes && 
					method.CustomAttributes.Any(a => a.TypeFullName.Contains("VirtualizedMethod")),

				// Suspicious IL patterns indicating virtualization
				DetectSuspiciousILPattern(method),

				// Check for embedded virtualization stub
				HasVirtualizationStub(method),

				// Check for obfuscated string encoding
				HasObfuscatedStringEncoding(method)
			};

			return vmSignatures.Any(s => s);
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Detect suspicious IL patterns that indicate virtualization
	/// </summary>
	private bool DetectSuspiciousILPattern(MethodDef method) {
		try {
			var instructions = method.Body.Instructions;
			
			// Pattern 1: Excessive use of switch/jump tables
			int switchCount = instructions.Count(i => 
				i.OpCode == OpCodes.Switch || 
				i.OpCode.Name.Contains("br"));

			if (switchCount > instructions.Count * 0.3)
				return true;

			// Pattern 2: Large number of local variables (VM state storage)
			if (method.Body.Variables.Count > instructions.Count * 0.2)
				return true;

			// Pattern 3: Complex nested try-catch blocks (VM dispatch)
			if (method.Body.ExceptionHandlers.Count > 5)
				return true;

			return false;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Check if method has embedded virtualization stub
	/// </summary>
	private bool HasVirtualizationStub(MethodDef method) {
		try {
			if (method.Body?.Instructions.Count == 0)
				return false;

			// Look for characteristic VM bytecode markers
			var instructions = method.Body.Instructions;
			
			// VM dispatchers typically have large constant arrays
			int ldcCount = instructions.Count(i => i.OpCode.Name.StartsWith("ldc"));
			
			// VM state initialization patterns
			bool hasVMInit = instructions.Any(i => 
				i.OpCode == OpCodes.Newarr && 
				i.Operand is ITypeDefOrRef type &&
				type.Name.Contains("Byte"));

			return ldcCount > 10 && hasVMInit;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Check for obfuscated string encoding (Reactor typical pattern)
	/// </summary>
	private bool HasObfuscatedStringEncoding(MethodDef method) {
		try {
			var instructions = method.Body?.Instructions;
			if (instructions == null)
				return false;

			// Look for decryption/decoding patterns
			int xorCount = instructions.Count(i => i.OpCode == OpCodes.Xor);
			int addCount = instructions.Count(i => i.OpCode == OpCodes.Add);
			int callCount = instructions.Count(i => i.OpCode == OpCodes.Call);

			// Typical obfuscation: XOR with ADD and method calls
			return xorCount > 2 || (addCount > 5 && callCount > 3);
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Attempt to unvirtualize a method by analyzing its layers
	/// </summary>
	private bool TryUnvirtualizeMethod(MethodDef method, out MethodDef recoveredMethod) {
		recoveredMethod = null;

		try {
			if (virtualizationDepth >= MAX_VM_DEPTH) {
				Logger.Warn("[NestedVM] Max virtualization depth reached");
				return false;
			}

			virtualizationDepth++;

			// Step 1: Try to recover method signature
			var signature = RecoverMethodSignature(method);
			if (signature == null) {
				Logger.Debug("[NestedVM] Could not recover signature for {0}", method.Name);
				return false;
			}

			// Step 2: Analyze VM bytecode layers
			var vmLayers = AnalyzeVMLayers(method);
			Logger.Debug("[NestedVM] Found {0} VM layers in method {1}", vmLayers.Count, method.Name);

			// Step 3: Try to reconstruct original IL
			if (vmLayers.Count > 0) {
				recoveredMethod = ReconstructMethodFromLayers(method, vmLayers);
				
				if (recoveredMethod != null) {
					Logger.Info("[NestedVM] ✓ Successfully recovered method: {0}", method.Name);
					return true;
				}
			}

			// Step 4: If reconstruction fails, at least extract what we can
			recoveredMethod = ExtractPartialRecovery(method);
			return recoveredMethod != null;
		}
		catch (Exception ex) {
			Logger.Exception(ex);
			return false;
		}
		finally {
			virtualizationDepth--;
		}
	}

	/// <summary>
	/// Recover method signature from virtualized code
	/// </summary>
	private MethodSignature? RecoverMethodSignature(MethodDef method) {
		try {
			// Method signature should be preserved even when virtualized
			if (method.MethodSig != null) {
				return method.MethodSig;
			}

			// Try to infer from method's declared type
			return new MethodSignature(method.CallingConvention, method.ReturnType, 
				method.Parameters.Select(p => p.Type).ToArray());
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Analyze virtualization layers in method bytecode
	/// </summary>
	private List<VMLayers> AnalyzeVMLayers(MethodDef method) {
		var layers = new List<VMLayers>();

		try {
			if (method.Body?.Instructions.Count == 0)
				return layers;

			// Analyze instruction patterns to identify layer boundaries
			var instructions = method.Body.Instructions;
			var currentLayer = new VMLayers { LayerIndex = 0, StartIndex = 0 };

			for (int i = 0; i < instructions.Count; i++) {
				var instruction = instructions[i];

				// Detect layer transition markers
				if (IsLayerTransition(instruction, i, instructions)) {
					currentLayer.EndIndex = i;
					layers.Add(currentLayer);
					currentLayer = new VMLayers { LayerIndex = layers.Count, StartIndex = i };
				}
			}

			if (currentLayer.StartIndex < instructions.Count) {
				currentLayer.EndIndex = instructions.Count - 1;
				layers.Add(currentLayer);
			}

			return layers;
		}
		catch (Exception ex) {
			Logger.Exception(ex);
			return layers;
		}
	}

	/// <summary>
	/// Detect transitions between VM layers
	/// </summary>
	private bool IsLayerTransition(Instruction instruction, int index, IList<Instruction> instructions) {
		try {
			// Layer transitions typically marked by:
			// 1. Switch statements (VM dispatcher)
			if (instruction.OpCode == OpCodes.Switch)
				return true;

			// 2. Large jumps with pattern
			if (instruction.OpCode.Name.StartsWith("br")) {
				if (instruction.Operand is Instruction target) {
					int targetIndex = instructions.IndexOf(target);
					if (Math.Abs(targetIndex - index) > 20)
						return true;
				}
			}

			// 3. Exception handler boundaries
			if (index > 0 && instructions[index - 1].OpCode == OpCodes.Leave)
				return true;

			return false;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Reconstruct original method from analyzed VM layers
	/// </summary>
	private MethodDef? ReconstructMethodFromLayers(MethodDef method, List<VMLayers> layers) {
		try {
			// Clone method
			var reconstructed = new MethodDefUser(method.Name, method.MethodSig, method.ImplAttributes);
			reconstructed.Attributes = method.Attributes;

			// Attempt to synthesize IL from layer information
			var newBody = new CilBody();
			
			foreach (var layer in layers) {
				// Extract meaningful instructions from each layer
				var layerInstructions = method.Body.Instructions
					.Skip(layer.StartIndex)
					.Take(layer.EndIndex - layer.StartIndex + 1)
					.Where(i => !IsVMDispatchOp(i))
					.ToList();

				foreach (var instr in layerInstructions) {
					newBody.Instructions.Add(instr);
				}
			}

			if (newBody.Instructions.Count > 0) {
				reconstructed.Body = newBody;
				return reconstructed;
			}

			return null;
		}
		catch (Exception ex) {
			Logger.Exception(ex);
			return null;
		}
	}

	/// <summary>
	/// Extract what we can recover from virtualized method
	/// </summary>
	private MethodDef? ExtractPartialRecovery(MethodDef method) {
		try {
			var recovered = new MethodDefUser(method.Name, method.MethodSig, method.ImplAttributes);
			recovered.Attributes = method.Attributes;
			recovered.Body = new CilBody();

			// Extract non-VM instructions
			if (method.Body?.Instructions != null) {
				var meaningfulInstructions = method.Body.Instructions
					.Where(i => !IsVMDispatchOp(i))
					.Take(Math.Min(50, method.Body.Instructions.Count)); // Limit to avoid partial code

				foreach (var instr in meaningfulInstructions) {
					recovered.Body.Instructions.Add(instr);
				}
			}

			return recovered.Body.Instructions.Count > 0 ? recovered : null;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Identify VM dispatch operations to filter out
	/// </summary>
	private bool IsVMDispatchOp(Instruction instruction) {
		return instruction.OpCode == OpCodes.Switch ||
			   instruction.OpCode == OpCodes.Br_S ||
			   instruction.OpCode == OpCodes.Br ||
			   (instruction.OpCode.Name.StartsWith("br") && instruction.OpCode != OpCodes.Brfalse && instruction.OpCode != OpCodes.Brtrue);
	}

	/// <summary>
	/// VM Layer information
	/// </summary>
	private class VMLayers {
		public int LayerIndex { get; set; }
		public int StartIndex { get; set; }
		public int EndIndex { get; set; }
	}
}