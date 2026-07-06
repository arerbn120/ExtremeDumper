using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.PE;

namespace ExtremeDumper.Utilities;

/// <summary>
/// Extension methods for dnlib 4.x compatibility and convenience
/// </summary>
public static class DnlibExtensions {
	/// <summary>
	/// Safe module loading with 4.x compatibility
	/// </summary>
	public static ModuleDefMD? TryLoadModule(byte[] data) {
		try {
			if (data == null || data.Length == 0)
				return null;

			using var peImage = new PEImage(data);
			return ModuleDefMD.Load(peImage);
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Safe module loading from file
	/// </summary>
	public static ModuleDefMD? TryLoadModuleFromFile(string filePath) {
		try {
			if (!System.IO.File.Exists(filePath))
				return null;

			var data = System.IO.File.ReadAllBytes(filePath);
			return TryLoadModule(data);
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Get all types including nested types recursively
	/// </summary>
	public static IEnumerable<TypeDef> GetAllTypes(this ModuleDef module) {
		if (module == null)
			yield break;

		foreach (var type in module.Types) {
			yield return type;
			foreach (var nestedType in type.GetAllNestedTypes()) {
				yield return nestedType;
			}
		}
	}

	/// <summary>
	/// Get all nested types recursively
	/// </summary>
	public static IEnumerable<TypeDef> GetAllNestedTypes(this TypeDef type) {
		if (type == null)
			yield break;

		foreach (var nested in type.NestedTypes) {
			yield return nested;
			foreach (var deepNested in nested.GetAllNestedTypes()) {
				yield return deepNested;
			}
		}
	}

	/// <summary>
	/// Get all methods in a type including properties and events
	/// </summary>
	public static IEnumerable<MethodDef> GetAllMethods(this TypeDef type) {
		if (type == null)
			return Enumerable.Empty<MethodDef>();

		var methods = new HashSet<MethodDef>(type.Methods);

		// Add property getter/setter methods
		foreach (var property in type.Properties) {
			if (property.GetMethod != null)
				methods.Add(property.GetMethod);
			if (property.SetMethod != null)
				methods.Add(property.SetMethod);
		}

		// Add event add/remove methods
		foreach (var eventDef in type.Events) {
			if (eventDef.AddMethod != null)
				methods.Add(eventDef.AddMethod);
			if (eventDef.RemoveMethod != null)
				methods.Add(eventDef.RemoveMethod);
			if (eventDef.InvokeMethod != null)
				methods.Add(eventDef.InvokeMethod);
		}

		return methods;
	}

	/// <summary>
	/// Check if method is virtualized based on IL patterns
	/// </summary>
	public static bool IsVirtualized(this MethodDef method) {
		if (method?.Body?.Instructions == null)
			return false;

		var instructions = method.Body.Instructions;
		if (instructions.Count < 3)
			return false;

		// Pattern 1: Excessive switch statements
		int switchCount = instructions.Count(i => i.OpCode == OpCodes.Switch);
		if (switchCount > instructions.Count * 0.2)
			return true;

		// Pattern 2: Many jumps relative to instruction count
		int branchCount = instructions.Count(i => i.OpCode.Name.StartsWith("br"));
		if (branchCount > instructions.Count * 0.3)
			return true;

		// Pattern 3: Very large number of locals (VM state)
		if (method.Body.Variables.Count > instructions.Count * 0.15)
			return true;

		// Pattern 4: Complex exception handlers
		if (method.Body.ExceptionHandlers.Count > 10)
			return true;

		// Pattern 5: Suspicious attribute indicating virtualization
		if (method.HasCustomAttributes) {
			if (method.CustomAttributes.Any(a => 
				a.TypeFullName.Contains("Virtualized") || 
				a.TypeFullName.Contains("Protected") ||
				a.TypeFullName.Contains("Obfuscated")))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Check if type contains virtualized methods
	/// </summary>
	public static bool HasVirtualizedMethods(this TypeDef type) {
		if (type == null)
			return false;

		return type.Methods.Any(m => m.IsVirtualized());
	}

	/// <summary>
	/// Get method's IL complexity score (0-100)
	/// </summary>
	public static int GetComplexityScore(this MethodDef method) {
		if (method?.Body?.Instructions == null)
			return 0;

		var instructions = method.Body.Instructions;
		int score = 0;

		// Base score from instruction count
		score += Math.Min(instructions.Count / 10, 20);

		// Local variables
		score += Math.Min(method.Body.Variables.Count * 2, 15);

		// Exception handlers
		score += method.Body.ExceptionHandlers.Count * 5;

		// Branches
		int branches = instructions.Count(i => i.OpCode.Name.StartsWith("br"));
		score += Math.Min(branches * 2, 25);

		// Method calls
		int calls = instructions.Count(i => i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt);
		score += Math.Min(calls / 2, 15);

		return Math.Min(score, 100);
	}

	/// <summary>
	/// Get method signature as readable string
	/// </summary>
	public static string GetReadableSignature(this MethodDef method) {
		if (method == null)
			return "<null>";

		var returnType = method.ReturnType?.FullName ?? "void";
		var parameters = method.Parameters
			.Where(p => p.Type != null)
			.Select(p => $"{p.Type.FullName} {p.Name}")
			.ToList();

		return $"{returnType} {method.Name}({string.Join(", ", parameters)})";
	}

	/// <summary>
	/// Safely resolve type reference
	/// </summary>
	public static TypeDef? ResolveType(this ITypeDefOrRef typeRef, ModuleDef module) {
		try {
			if (typeRef == null)
				return null;

			if (typeRef is TypeDef typeDef)
				return typeDef;

			if (typeRef is TypeRef typeRef2) {
				return typeRef2.Resolve();
			}

			return null;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Check if method has specific custom attribute
	/// </summary>
	public static bool HasCustomAttribute(this MethodDef method, string attributeName) {
		if (method == null)
			return false;

		return method.CustomAttributes.Any(a => 
			a.TypeFullName.EndsWith(attributeName, StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Get all string literals used in method
	/// </summary>
	public static IEnumerable<string> GetStringLiterals(this MethodDef method) {
		if (method?.Body?.Instructions == null)
			yield break;

		foreach (var instruction in method.Body.Instructions) {
			if (instruction.OpCode == OpCodes.Ldstr && instruction.Operand is string str) {
				yield return str;
			}
		}
	}

	/// <summary>
	/// Clone a method definition
	/// </summary>
	public static MethodDef Clone(this MethodDef method) {
		var cloned = new MethodDefUser(
			method.Name,
			method.MethodSig,
			method.ImplAttributes
		) {
			Attributes = method.Attributes
		};

		if (method.Body != null) {
			var newBody = new CilBody(method.Body.InitLocals, 
				new List<Instruction>(method.Body.Instructions),
				new List<ExceptionHandler>(method.Body.ExceptionHandlers),
				new List<Local>(method.Body.Variables));
			cloned.Body = newBody;
		}

		return cloned;
	}

	/// <summary>
	/// Get module's assembly name
	/// </summary>
	public static string GetAssemblyName(this ModuleDef module) {
		if (module?.Assembly != null)
			return module.Assembly.Name;
		return module?.Name ?? "<unknown>";
	}

	/// <summary>
	/// Check if assembly is obfuscated based on heuristics
	/// </summary>
	public static bool LooksObfuscated(this ModuleDef module) {
		if (module == null)
			return false;

		int obfuscationIndicators = 0;

		// Check 1: High proportion of methods with suspicious names
		var suspiciousNames = module.GetAllTypes()
			.SelectMany(t => t.Methods)
			.Count(m => m.Name.Length <= 3 || m.Name.All(c => c == '_'));

		if (suspiciousNames > module.GetAllTypes().SelectMany(t => t.Methods).Count() * 0.3)
			obfuscationIndicators++;

		// Check 2: Many virtualized methods
		var virtualizedCount = module.GetAllTypes()
			.SelectMany(t => t.Methods)
			.Count(m => m.IsVirtualized());

		if (virtualizedCount > module.GetAllTypes().SelectMany(t => t.Methods).Count() * 0.2)
			obfuscationIndicators++;

		// Check 3: Unusual metadata
		if (module.CustomAttributes.Count > 50)
			obfuscationIndicators++;

		// Check 4: Complex resource structure
		if (module.Resources.Count > 10)
			obfuscationIndicators++;

		return obfuscationIndicators >= 2;
	}

	/// <summary>
	/// Safe instruction operand access
	/// </summary>
	public static T? GetOperandAs<T>(this Instruction instruction) where T : class {
		try {
			return instruction?.Operand as T;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Get instruction IL offset as hex
	/// </summary>
	public static string GetILOffsetHex(this Instruction instruction) {
		try {
			return $"0x{instruction?.Offset:X4}";
		}
		catch {
			return "0x????";
		}
	}

	/// <summary>
	/// Filter methods by complexity level
	/// </summary>
	public static IEnumerable<MethodDef> FilterByComplexity(
		this IEnumerable<MethodDef> methods, 
		int minComplexity = 0, 
		int maxComplexity = 100) {
		return methods.Where(m => {
			int score = m.GetComplexityScore();
			return score >= minComplexity && score <= maxComplexity;
		});
	}
}