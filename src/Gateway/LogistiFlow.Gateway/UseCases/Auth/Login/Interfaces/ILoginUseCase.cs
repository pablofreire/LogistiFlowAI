using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Gateway.UseCases.Auth.Login.Contracts;

namespace LogistiFlow.Gateway.UseCases.Auth.Login.Interfaces;

public interface ILoginUseCase : IUseCase<LoginRequest, LoginResponse>;
