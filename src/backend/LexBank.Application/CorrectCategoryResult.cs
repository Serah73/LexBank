using LexBank.Domain;

namespace LexBank.Application;

public record CorrectCategoryResult(Transaction Transaction, Accuracy Accuracy);
