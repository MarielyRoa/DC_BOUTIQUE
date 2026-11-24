namespace DC_BOUTIQUE.Core.Application.Wrappers
{
    public class Result
    {
        public bool Succeeded { get; protected set; }
        public string? Message { get; protected set; }
        public List<string> Errors { get; protected set; } = new();

        public bool IsSuccess => Succeeded;
        public bool IsFailure => !Succeeded;

        protected Result() { }

        public static Result Success(string? message = null)
        {
            return new Result
            {
                Succeeded = true,
                Message = message
            };
        }

        public static Result Failure(string errorMessage)
        {
            return new Result
            {
                Succeeded = false,
                Message = errorMessage,
                Errors = new List<string> { errorMessage }
            };
        }

        public static Result Failure(IEnumerable<string> errors, string? message = null)
        {
            var errList = errors.ToList();
            return new Result
            {
                Succeeded = false,
                Message = message ?? (errList.Count > 0 ? errList[0] : "Ocurrió un error inesperado."),
                Errors = errList
            };
        }
    }

    public class Result<T> : Result
    {
        public T? Data { get; private set; }
        public T? Value => Data;

        protected Result() { }

        public static Result<T> Success(T data, string? message = null)
        {
            return new Result<T>
            {
                Succeeded = true,
                Data = data,
                Message = message
            };
        }

        public new static Result<T> Failure(string errorMessage)
        {
            return new Result<T>
            {
                Succeeded = false,
                Message = errorMessage,
                Errors = new List<string> { errorMessage }
            };
        }

        public new static Result<T> Failure(IEnumerable<string> errors, string? message = null)
        {
            var errList = errors.ToList();
            return new Result<T>
            {
                Succeeded = false,
                Message = message ?? (errList.Count > 0 ? errList[0] : "Ocurrió un error inesperado."),
                Errors = errList
            };
        }

        public static implicit operator Result<T>(T value) => Success(value);
    }
}
