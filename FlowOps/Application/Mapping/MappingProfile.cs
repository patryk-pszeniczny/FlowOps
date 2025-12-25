using AutoMapper;
using FlowOps.Application.Customers.Queries;
using FlowOps.Contracts.Item;
using FlowOps.Contracts.Response;
using FlowOps.Domain.Customers;
using FlowOps.Domain.Subscriptions;
using FlowOps.Infrastructure.Persistence.Entities;

namespace FlowOps.Application.Mapping
{
    public sealed class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<Customer, CustomerDto>();

            CreateMap<Subscription, SubscriptionDetailsResponse>()
                .ForCtorParam("id", opt => opt.MapFrom(src => src.Id))
                .ForCtorParam("CustomerId", opt => opt.MapFrom(src => src.CustomerId))
                .ForCtorParam("PlanCode", opt => opt.MapFrom(src => src.PlanCode))
                .ForCtorParam("Status", opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<Subscription, SubscriptionListItem>()
                .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id))
                .ForCtorParam("PlanCode", opt => opt.MapFrom(src => src.PlanCode))
                .ForCtorParam("Status", opt => opt.MapFrom(src => src.Status.ToString()));

            CreateMap<Subscription, SubscriptionSqlResponse>()
                .ForCtorParam("SubscriptionId", opt => opt.MapFrom(src => src.Id))
                .ForCtorParam("CustomerId", opt => opt.MapFrom(src => src.CustomerId))
                .ForCtorParam("PlanCode", opt => opt.MapFrom(src => src.PlanCode))
                .ForCtorParam("Status", opt => opt.MapFrom(src => src.Status.ToString()))
                .ForCtorParam("ActivatedAt", opt => opt.MapFrom(src => src.ActivatedAt ?? DateTime.MinValue))
                .ForCtorParam("SuspendedAt", opt => opt.MapFrom(src => src.SuspendedAt))
                .ForCtorParam("ResumedAt", opt => opt.MapFrom(src => src.ResumedAt))
                .ForCtorParam("CancelledAt", opt => opt.MapFrom(src => src.CancelledAt));

            CreateMap<CustomerReport, CustomerReportSqlResponse>();

        }
    }
}
