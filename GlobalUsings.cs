// Disambiguates `Application` project-wide. With UseWPF + UseWindowsForms +
// ImplicitUsings, both System.Windows.Application (WPF) and
// System.Windows.Forms.Application end up in scope, making the bare name
// ambiguous. We always want WPF's.
global using Application = System.Windows.Application;
