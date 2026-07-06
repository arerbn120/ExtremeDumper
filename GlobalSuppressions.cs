// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1806:Do not ignore method results", 
	Justification = "Intentional - some method results are not used")]

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", 
	Justification = "Required for robust error handling in dumper operations")]

[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", 
	Justification = "IDisposable objects are properly managed via using statements")]

[assembly: SuppressMessage("Interoperability", "CA1401:P/Invokes should not be visible", 
	Justification = "P/Invoke methods are intentionally exposed for native interop")]

[assembly: SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", 
	Justification = "Some disposable fields are managed by parent classes")]

[assembly: SuppressMessage("Security", "CA2104:Do not declare read only mutable reference types", 
	Justification = "Intentional for specific performance-critical scenarios")]

[assembly: SuppressMessage("Naming", "CA1711:Identifiers should not have incorrect suffix", 
	Justification = "Names are intentional and domain-specific")]

[assembly: SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", 
	Justification = "Underscores used for internal/private implementation")]