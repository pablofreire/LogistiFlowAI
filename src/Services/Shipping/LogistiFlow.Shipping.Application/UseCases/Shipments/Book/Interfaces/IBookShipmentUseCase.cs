using LogistiFlow.BuildingBlocks.Application.UseCases.Interfaces;
using LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Contracts;

namespace LogistiFlow.Shipping.Application.UseCases.Shipments.Book.Interfaces;

public interface IBookShipmentUseCase : IUseCase<BookShipmentRequest, BookShipmentResponse>;
