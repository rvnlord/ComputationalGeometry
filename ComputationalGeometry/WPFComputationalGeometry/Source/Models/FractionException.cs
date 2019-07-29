using System;

namespace WPFComputationalGeometry.Source.Models
{
    public class FractionException : Exception
    {
        public FractionException()
        {
            Message = "Niepoprawny Ułamek.";
        }

        public FractionException(string message)
            : base(message)
        {
        }

        public FractionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        public override string Message { get; }
    }
}
