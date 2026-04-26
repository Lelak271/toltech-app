namespace Toltech.App.Utilities.Result
{
    public class ResultPattern
    {
    }

    // Result non-générique (opérations sans valeur de retour)
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; }
        public ErrorCode Code { get; }

        protected Result(bool isSuccess, string error, ErrorCode code)
        {
            IsSuccess = isSuccess;
            Error = error;
            Code = code;
        }

        public static Result Success()
            => new Result(true, string.Empty, ErrorCode.None);

        public static Result Failure(string error, ErrorCode code = ErrorCode.Unknown)
            => new Result(false, error, code);

        // Conversion implicite depuis Result<T> → Result
        public static implicit operator Result(string error)
            => Failure(error);
    }

    // Result générique (opérations retournant une valeur)
    public class Result<T> : Result
    {
        public T Value { get; }

        private Result(T value, bool isSuccess, string error, ErrorCode code)
            : base(isSuccess, error, code)
        {
            Value = value;
        }

        public static Result<T> Success(T value)
            => new Result<T>(value, true, string.Empty, ErrorCode.None);

        public static new Result<T> Failure(string error, ErrorCode code = ErrorCode.Unknown)
            => new Result<T>(default, false, error, code);
    }

    public class ValidationResult
    {
        public bool IsValid { get; }
        public bool HasErrors => !IsValid;
        public IReadOnlyList<string> Errors { get; }
        public string SuccessMessage { get; }

        private ValidationResult(bool isValid, List<string> errors, string successMessage)
        {
            IsValid = isValid;
            Errors = errors.AsReadOnly();
            SuccessMessage = successMessage;
        }

        public static ValidationResult Valid(string successMessage)
            => new ValidationResult(true, new List<string>(), successMessage);

        public static ValidationResult Invalid(List<string> errors)
            => new ValidationResult(false, errors, string.Empty);
    }

    // Codes d'erreur métier (à enrichir selon votre domaine)
    public enum ErrorCode
    {
        None,
        Unknown,
        NotFound,
        ValidationError,
        DatabaseError,
        Unauthorized,
        DuplicateEntry,
        FileLocked,
        InvalidPath,
        InvalidInput,
        NoActiveModel,
        BusinessRuleViolation
    }

}
