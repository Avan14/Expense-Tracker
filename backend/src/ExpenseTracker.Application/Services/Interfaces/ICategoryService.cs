using ExpenseTracker.Application.DTOs.Categories;
using ExpenseTracker.Domain.Enums;

namespace ExpenseTracker.Application.Services.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoriesAsync(Guid userId, TransactionType? type = null, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(Guid userId, CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateCategoryAsync(Guid userId, Guid categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid userId, Guid categoryId, Guid? reassignToCategoryId = null, CancellationToken cancellationToken = default);
}
