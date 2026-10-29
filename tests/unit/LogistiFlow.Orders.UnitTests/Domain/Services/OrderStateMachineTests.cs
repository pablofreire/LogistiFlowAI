using LogistiFlow.Orders.Domain.Enums;
using LogistiFlow.Orders.Domain.Services;

namespace LogistiFlow.Orders.UnitTests.Domain.Services;

public sealed class OrderStateMachineTests
{
    private static readonly (OrderStatus From, OrderStatus To)[] Allowed =
    [
        (OrderStatus.Created, OrderStatus.PendingInventory),
        (OrderStatus.Created, OrderStatus.Cancelled),
        (OrderStatus.PendingInventory, OrderStatus.Reserved),
        (OrderStatus.PendingInventory, OrderStatus.Failed),
        (OrderStatus.PendingInventory, OrderStatus.Cancelled),
        (OrderStatus.Reserved, OrderStatus.Picking),
        (OrderStatus.Reserved, OrderStatus.Cancelled),
        (OrderStatus.Picking, OrderStatus.Packed),
        (OrderStatus.Picking, OrderStatus.Failed),
        (OrderStatus.Packed, OrderStatus.Shipped),
        (OrderStatus.Packed, OrderStatus.Failed),
        (OrderStatus.Shipped, OrderStatus.Delivered),
        (OrderStatus.Shipped, OrderStatus.Failed),
    ];

    public static TheoryData<OrderStatus, OrderStatus> AllPairs()
    {
        var data = new TheoryData<OrderStatus, OrderStatus>();
        foreach (var from in Enum.GetValues<OrderStatus>())
        {
            foreach (var to in Enum.GetValues<OrderStatus>())
            {
                data.Add(from, to);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllPairs))]
    public void CanTransition_EveryPair_MatchesTheDocumentedTable(OrderStatus from, OrderStatus to)
    {
        var expected = Allowed.Contains((from, to));

        OrderStateMachine.CanTransition(from, to).Should().Be(expected, $"{from} -> {to}");
    }

    [Theory]
    [InlineData(OrderStatus.Delivered)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Failed)]
    public void TerminalStatuses_AllowNoTransition(OrderStatus status)
    {
        OrderStateMachine.IsTerminal(status).Should().BeTrue();
        OrderStateMachine.AllowedTargets(status).Should().BeEmpty();
    }

    [Fact]
    public void ProgressRank_FollowsTheHappyPath()
    {
        var path = new[]
        {
            OrderStatus.Created, OrderStatus.PendingInventory, OrderStatus.Reserved, OrderStatus.Picking,
            OrderStatus.Packed, OrderStatus.Shipped, OrderStatus.Delivered,
        };

        path.Select(OrderStateMachine.ProgressRank).Should().BeInAscendingOrder();
    }
}
