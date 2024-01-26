namespace OpenMedStack.SparkEngine.Core;

using System;
using System.Runtime.Serialization;

[Serializable]
internal class RequiredAttributeException : Exception
{
    public RequiredAttributeException()
    {
    }

    public RequiredAttributeException(string message) : base(message)
    {
    }

    public RequiredAttributeException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
