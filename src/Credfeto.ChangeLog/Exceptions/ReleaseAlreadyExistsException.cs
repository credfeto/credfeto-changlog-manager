using System;
using System.ComponentModel;

namespace Credfeto.ChangeLog.Exceptions;

[Description("Release already exists.")]
public sealed partial class ReleaseAlreadyExistsException : Exception;
