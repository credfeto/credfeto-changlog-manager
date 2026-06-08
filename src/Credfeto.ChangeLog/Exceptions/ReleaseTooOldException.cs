using System;
using System.ComponentModel;

namespace Credfeto.ChangeLog.Exceptions;

[Description("Release is older than the current release.")]
public sealed partial class ReleaseTooOldException : Exception;
