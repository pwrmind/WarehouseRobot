# Warehouse Robot Simulation with Behavior Tree

This project simulates a warehouse robot navigating a grid environment with obstacles to deliver a package. The robot uses a Behavior Tree to make decisions about movement, obstacle avoidance, and package delivery.

## Features

- Behavior Tree implementation for AI decision-making
- Obstacle detection and avoidance
- Pathfinding to target location
- Package pickup and delivery system
- Grid-based environment simulation
- Real-time simulation output

## Problem Solved

The initial implementation had a critical issue where the robot would get stuck at position (2,2) facing West. This happened because:

1. The robot's turning logic couldn't decide between left/right turns when target displacements were equal (|dx| = |dy|)
2. The Behavior Tree lacked fallback strategies for such cases

The solution added an unconditional left turn as a fallback strategy in the turning logic to prevent deadlocks.

## Code Structure

### Key Components
- `Program.cs`: Main entry point and simulation loop
- `INode.cs`: Behavior Tree node interface
- `CompositeNode.cs`: Base class for composite nodes
- `ActionNode.cs`: Node for executing actions
- `ConditionNode.cs`: Node for checking conditions
- `Selector.cs`: Composite node that executes children until one succeeds
- `Sequence.cs`: Composite node that executes all children in sequence
- `BehaviourTreeBuilder.cs`: Fluent API for building Behavior Trees
- `RobotBehaviourTree.cs`: Implements the robot's specific Behavior Tree
- `WarehouseRobot.cs`: Robot logic and state management

### Behavior Tree Structure
The robot's Behavior Tree is structured as:
```
Selector (root)
├── Sequence: Deliver package
│   ├── Condition: At target?
│   ├── Condition: Has package?
│   └── Action: Deliver package
└── Sequence: Navigate to target
    ├── Condition: Not at target
    └── Selector: Movement strategies
        ├── Sequence: Move forward
        ├── Sequence: Avoid obstacle
        └── Selector: Turning strategies
            ├── Sequence: Turn right
            ├── Sequence: Turn left
            └── Sequence: Turn left (fallback)
```

## Getting Started

### Prerequisites
- .NET 6 SDK or newer

### Running the Simulation
1. Clone the repository:
```bash
git clone https://github.com/pwrmind/WarehouseRobot.git
cd WarehouseRobot
```

2. Run the program:
```bash
dotnet run
```

### Sample Output
```
Подобрал груз!
[Такт 0]: Робот на (1,1), смотрит East. 
Переместился на [2,1]
[Такт 1]: Робот на (2,1), смотрит East. 
Переместился на [3,1]
[Такт 2]: Робот на (3,1), смотрит East. 
Переместился на [4,1]
[Такт 3]: Робот на (4,1), смотрит East. 
Повернул налево, теперь смотрю North
Переместился на [4,2]
[Такт 4]: Робот на (4,2), смотрю North. 
Повернул налево, теперь смотрю West
Переместился на [3,2]
[Такт 5]: Робот на (3,2), смотрю West. 
Повернул направо, теперь смотрю North
[Такт 6]: Робот на (3,2), смотрю North. 
Повернул налево, теперь смотрю West
Переместился на [2,2]
[Такт 7]: Робот на (2,2), смотрю West. 
Повернул налево, теперь смотрю South
Переместился на [2,1]
... (continues to target)
Доставка завершена!
```

## Customization

You can modify the simulation parameters in `Program.cs`:
```csharp
// Change obstacles
var obstacles = new List<(int, int)>
{
    (2, 3), (3, 3), (4, 3),  // horizontal wall
    (5, 1), (5, 2), (5, 3)    // vertical blocks
};

// Change robot starting position and target
var robot = new WarehouseRobot(
    startX: 1, 
    startY: 1, 
    startDir: Direction.East,
    targetX: 6,
    targetY: 6,
    obstacles: obstacles
);
```

## Key Improvements

1. Added fallback turning strategy to prevent deadlocks
2. Enhanced obstacle avoidance logic
3. Improved pathfinding through behavior tree optimizations

## Contributing

Contributions are welcome! Please open an issue to discuss your proposed changes before submitting a pull request.