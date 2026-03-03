namespace App.CrossCutting.Validator;

public interface IValidator<in T> : FluentValidation.IValidator<T>;