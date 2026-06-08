using System;
using System.ComponentModel;

namespace Credfeto.ChangeLog.Exceptions;

[Description("Changelog does not contain content.")]
public sealed partial class EmptyChangeLogException : Exception;
