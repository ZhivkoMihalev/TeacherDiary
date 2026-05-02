using TeacherDiary.Application.Common;
using TeacherDiary.Application.DTOs.Auth;

namespace TeacherDiary.Application.Abstractions.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterTeacherAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<Result<AuthResponse>> RegisterParentAsync(RegisterParentRequest request, CancellationToken cancellationToken);

    Task<Result<AuthResponse>> RegisterStudentAsync(RegisterStudentRequest request, CancellationToken cancellationToken);

    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
}
