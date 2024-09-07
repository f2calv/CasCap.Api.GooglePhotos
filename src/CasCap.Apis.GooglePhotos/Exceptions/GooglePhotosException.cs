﻿using System.Runtime.Serialization;

namespace CasCap.Exceptions;

[Serializable]
public class GooglePhotosException : Exception
{
    public GooglePhotosException() { }
    public GooglePhotosException(string message) : base(message) { }
    public GooglePhotosException(string message, Exception? innerException) : base(message, innerException) { }

    [Obsolete("added to pass sonarqube", DiagnosticId = "SYSLIB0051")]
    protected GooglePhotosException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    [Obsolete("added to pass sonarqube", DiagnosticId = "SYSLIB0051")]
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
    }
}
