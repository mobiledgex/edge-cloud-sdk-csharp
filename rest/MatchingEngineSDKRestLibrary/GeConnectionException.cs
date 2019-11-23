using System;
namespace DistributedMatchEngine
{
  public class GetConnectionException : Exception
  {
        public GetConnectionException()
        {
        }

        public GetConnectionException(string message)
        : base(message)
        {
        }

        public GetConnectionException(string message, Exception inner)
        : base(message, inner)
        {
        }
  }
}
