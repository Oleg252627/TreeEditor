namespace TreeEditor.Application.Common.Validation;

public interface IValidator<in T>
{
    void Validate(T value);
}