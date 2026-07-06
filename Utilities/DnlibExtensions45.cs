using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.PE;

namespace ExtremeDumper.Utilities;

/// <summary>
/// Extension methods for dnlib 4.5.x specific features and improvements
/// </summary>
public static class DnlibExtensions45 {
	/// <summary>
	/// Load module with enhanced error handling for dnlib 4.5
	/// </summary>
	public static ModuleDefMD? LoadModuleSafely(byte[] data, ModuleCreationOptions options = null!) {
		try {
			if (data == null || data.Length == 0)
				return null;

			using var peImage = new PEImage(data);
			
			// dnlib 4.5: ModuleDefMD.Load supports null options (uses defaults)
			if (options == null) {
				return ModuleDefMD.Load(peImage);
			}
			
			return ModuleDefMD.Load(peImage, options);
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Get method's IL body with proper 4.5 compatibility
	/// </summary>
	public static CilBody? GetMethodBody(this MethodDef method) {
		try {
			// dnlib 4.5: Improved null safety
			if (method?.Body == null)
				return null;

			return method.Body;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Enhanced type resolution with 4.5 improvements
	/// </summary>
	public static TypeDef? ResolveTypeSafe(this ITypeDefOrRef typeRef) {
		try {
			if (typeRef == null)
				return null;

			// dnlib 4.5: Better null handling in type resolution
			return typeRef switch {
				TypeDef td => td,
				TypeRef tr => tr.Resolve(),
				TypeSpec ts => null, // TypeSpec can't be directly resolved
				_ => null
			};
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Get all custom attributes with dnlib 4.5 optimizations
	/// </summary>
	public static IEnumerable<CustomAttribute> GetAllCustomAttributes(this IHasCustomAttribute member) {
		try {
			if (member?.CustomAttributes == null)
				return Enumerable.Empty<CustomAttribute>();

			// dnlib 4.5: Improved enumeration performance
			return member.CustomAttributes.AsEnumerable();
		}
		catch {
			return Enumerable.Empty<CustomAttribute>();
		}
	}

	/// <summary>
	/// Check if type is virtualized using improved 4.5 APIs
	/// </summary>
	public static bool IsTypeVirtualized(this TypeDef type) {
		try {
			if (type == null)
				return false;

			// Check for virtualization markers in 4.5
			var virtualizedMethods = type.Methods
				.Where(m => m.IsVirtualized())
				.Count();

			if (virtualizedMethods > 0 && virtualizedMethods > type.Methods.Count / 3)
				return true;

			// Check custom attributes
			if (type.HasCustomAttributes) {
				if (type.CustomAttributes.Any(a =>
					a.TypeFullName.Contains("Virtualized") ||
					a.TypeFullName.Contains("Protected") ||
					a.TypeFullName.Contains("Obfuscated")))
					return true;
			}

			return false;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Get member visibility with dnlib 4.5 enhancements
	/// </summary>
	public static MemberVisibility GetVisibility(this IMemberDef member) {
		try {
			// dnlib 4.5: Improved access modifier detection
			if (member is IAccessible accessible) {
				return accessible.Access switch {
					MethodAttributes.Private => MemberVisibility.Private,
					MethodAttributes.FamANDAssem => MemberVisibility.PrivateProtected,
					MethodAttributes.Assembly => MemberVisibility.Internal,
					MethodAttributes.Family => MemberVisibility.Protected,
					MethodAttributes.FamORAssem => MemberVisibility.ProtectedInternal,
					MethodAttributes.Public => MemberVisibility.Public,
					_ => MemberVisibility.Private
				};
			}

			return MemberVisibility.Private;
		}
		catch {
			return MemberVisibility.Private;
		}
	}

	/// <summary>
	/// Get instruction operand with improved 4.5 type safety
	/// </summary>
	public static T? GetOperandSafely<T>(this Instruction instruction) where T : class {
		try {
			if (instruction?.Operand == null)
				return null;

			// dnlib 4.5: Better operand type checking
			if (instruction.Operand is T typed)
				return typed;

			return null;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Clone assembly with dnlib 4.5 improvements
	/// </summary>
	public static AssemblyDef? CloneAssembly(this AssemblyDef assembly) {
		try {
			if (assembly == null)
				return null;

			// dnlib 4.5: Improved assembly cloning
			var cloned = new AssemblyDefUser(assembly.Name, assembly.Version, assembly.PublicKey) {
				Culture = assembly.Culture,
				AssemblyAttributes = assembly.Attributes,
				HashAlgorithm = assembly.HashAlgorithm
			};

			// Copy modules
			foreach (var module in assembly.Modules) {
				cloned.Modules.Add(module);
			}

			// Copy custom attributes
			foreach (var attr in assembly.CustomAttributes) {
				cloned.CustomAttributes.Add(attr);
			}

			return cloned;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Check for dnlib 4.5 metadata corruption
	/// </summary>
	public static bool HasMetadataIssues(this ModuleDef module) {
		try {
			if (module == null)
				return true;

			int issues = 0;

			// Check 1: Invalid method references
			foreach (var type in module.GetTypes()) {
				foreach (var method in type.Methods) {
					if (method.Signature == null)
						issues++;
				}
			}

			// Check 2: Circular type references
			var visitedTypes = new HashSet<TypeDef>();
			foreach (var type in module.GetTypes()) {
				if (!CheckTypeCircular(type, visitedTypes, new HashSet<TypeDef>()))
					issues++;
			}

			// Check 3: Invalid token values
			foreach (var type in module.GetTypes()) {
				if (type.MDToken.Raw == 0)
					issues++;
			}

			return issues > 0;
		}
		catch {
			return true;
		}
	}

	private static bool CheckTypeCircular(TypeDef type, HashSet<TypeDef> visited, HashSet<TypeDef> recursionStack) {
		if (recursionStack.Contains(type))
			return false; // Circular reference detected

		if (visited.Contains(type))
			return true;

		visited.Add(type);
		recursionStack.Add(type);

		foreach (var nestedType in type.NestedTypes) {
			if (!CheckTypeCircular(nestedType, visited, recursionStack))
				return false;
		}

		recursionStack.Remove(type);
		return true;
	}

	/// <summary>
	/// Get method's parameter information with dnlib 4.5
	/// </summary>
	public static IEnumerable<ParameterInfo> GetParameterInfo(this MethodDef method) {
		try {
			if (method?.Signature == null)
				yield break;

			var sig = method.Signature;
			
			// dnlib 4.5: Improved parameter enumeration
			for (int i = 0; i < sig.Params.Count; i++) {
				var paramType = sig.Params[i];
				var paramDef = method.Parameters.FirstOrDefault(p => p.Sequence - 1 == i);

				yield return new ParameterInfo {
					Index = i,
					Name = paramDef?.Name ?? $"param{i}",
					Type = paramType,
					Attributes = paramDef?.Attributes ?? 0
				};
			}
		}
		catch {
			yield break;
		}
	}

	/// <summary>
	/// Find methods by signature pattern (dnlib 4.5)
	/// </summary>
	public static IEnumerable<MethodDef> FindMethodsBySignature(this ModuleDef module, string returnType, params string[] paramTypes) {
		try {
			if (module == null)
				yield break;

			// dnlib 4.5: Improved signature matching
			foreach (var type in module.GetAllTypes()) {
				foreach (var method in type.Methods) {
					if (method.Signature == null)
						continue;

					if (!method.Signature.RetType.FullName.EndsWith(returnType, StringComparison.OrdinalIgnoreCase))
						continue;

					if (method.Signature.Params.Count != paramTypes.Length)
						continue;

					bool paramsMatch = true;
					for (int i = 0; i < paramTypes.Length; i++) {
						if (!method.Signature.Params[i].FullName.EndsWith(paramTypes[i], StringComparison.OrdinalIgnoreCase)) {
							paramsMatch = false;
							break;
						}
					}

					if (paramsMatch)
						yield return method;
				}
			}
		}
		catch {
			yield break;
		}
	}

	/// <summary>
	/// Get all generic parameters with dnlib 4.5
	/// </summary>
	public static IEnumerable<GenericParam> GetAllGenericParameters(this MethodDef method) {
		try {
			if (method?.GenericParameters == null)
				return Enumerable.Empty<GenericParam>();

			// dnlib 4.5: Improved generic parameter handling
			return method.GenericParameters.AsEnumerable();
		}
		catch {
			return Enumerable.Empty<GenericParam>();
		}
	}

	/// <summary>
	/// Check method for pinvoke with dnlib 4.5
	/// </summary>
	public static bool IsPInvokeMethod(this MethodDef method) {
		try {
			// dnlib 4.5: Better P/Invoke detection
			if (method == null)
				return false;

			return method.ImplAttributes.HasFlag(MethodImplAttributes.InternalCall) ||
				   method.ImplAttributes.HasFlag(MethodImplAttributes.Native) ||
				   method.PInvokeImpl != null;
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Get P/Invoke information with dnlib 4.5
	/// </summary>
	public static PInvokeInfo? GetPInvokeInfo(this MethodDef method) {
		try {
			if (method == null)
				return null;

			// dnlib 4.5: Improved P/Invoke info access
			return method.PInvokeImpl;
		}
		catch {
			return null;
		}
	}

	/// <summary>
	/// Enumerate all method overrides (dnlib 4.5)
	/// </summary>
	public static IEnumerable<MethodOverride> GetMethodOverrides(this TypeDef type) {
		try {
			if (type?.Overrides == null)
				return Enumerable.Empty<MethodOverride>();

			// dnlib 4.5: Improved override enumeration
			return type.Overrides.AsEnumerable();
		}
		catch {
			return Enumerable.Empty<MethodOverride>();
		}
	}

	/// <summary>
	/// Check if method is compiler-generated (dnlib 4.5)
	/// </summary>
	public static bool IsCompilerGenerated(this MethodDef method) {
		try {
			if (method == null)
				return false;

			// dnlib 4.5: Detect compiler-generated methods
			return method.HasCustomAttributes &&
				   method.CustomAttributes.Any(a =>
					   a.TypeFullName.EndsWith("CompilerGeneratedAttribute", StringComparison.OrdinalIgnoreCase));
		}
		catch {
			return false;
		}
	}

	/// <summary>
	/// Get method's exception handlers with dnlib 4.5
	/// </summary>
	public static IEnumerable<ExceptionHandler> GetExceptionHandlers(this MethodDef method) {
		try {
			if (method?.Body?.ExceptionHandlers == null)
				return Enumerable.Empty<ExceptionHandler>();

			// dnlib 4.5: Improved exception handler enumeration
			return method.Body.ExceptionHandlers.AsEnumerable();
		}
		catch {
			return Enumerable.Empty<ExceptionHandler>();
		}
	}
}

/// <summary>
/// Parameter information structure
/// </summary>
public struct ParameterInfo {
	public int Index { get; set; }
	public string Name { get; set; }
	public TypeSig Type { get; set; }
	public ParameterAttributes Attributes { get; set; }
}

/// <summary>
/// Member visibility enumeration
/// </summary>
public enum MemberVisibility {
	Private,
	PrivateProtected,
	Internal,
	Protected,
	ProtectedInternal,
	Public
}