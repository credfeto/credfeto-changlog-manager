using System;
using System.ComponentModel;

namespace Credfeto.ChangeLog.Exceptions;

[Description("Could not find branch")]
public sealed partial class BranchMissingException : Exception;
