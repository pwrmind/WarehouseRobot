public class Program
{
    public static void Main()
    {
        // Создаем окружение с препятствиями
        var obstacles = new List<(int, int)>
        {
            (2, 3), (3, 3), (4, 3),  // горизонтальная стена
            (5, 1), (5, 2), (5, 3)   // вертикальные блоки
        };

        // Инициализируем робота:
        var robot = new WarehouseRobot(1, 1, Direction.East, 6, 6, obstacles);
        robot.PickPackage();

        var behaviourTree = new RobotBehaviourTree(robot).BuildTree();

        int tickCount = 0;
        const int maxTicks = 100; // Защита от бесконечного цикла

        while (!robot.IsPackageDelivered() && tickCount < maxTicks)
        {
            Console.Write($"[Такт {tickCount}]: ");
            Console.WriteLine($"Робот на ({robot.X},{robot.Y}), смотрит {robot.Facing}. ");

            behaviourTree.Execute();

            tickCount++;
            Thread.Sleep(300);
        }

        Console.WriteLine(robot.IsPackageDelivered()
            ? "Доставка завершена!"
            : "Доставка не удалась");
    }
}

public interface INode
{
    bool Execute();
}

public abstract class CompositeNode : INode
{
    protected List<INode> Children = new List<INode>();
    public CompositeNode AddChild(INode node) 
    {
        Children.Add(node);
        return this;
    }
    
    public abstract bool Execute();
}

public class ActionNode : INode
{
    private readonly Func<bool> _action;
    public ActionNode(Func<bool> action) => _action = action;
    public bool Execute() => _action();
}

public class ConditionNode : INode
{
    private readonly Func<bool> _condition;
    public ConditionNode(Func<bool> condition) => _condition = condition;
    public bool Execute() => _condition();
}

public class Selector : CompositeNode
{
    public override bool Execute() => Children.Any(child => child.Execute());
}

public class Sequence : CompositeNode
{
    public override bool Execute() => Children.All(child => child.Execute());
}

public class BehaviourTreeBuilder
{
    private readonly Stack<CompositeNode> _compositeStack = new Stack<CompositeNode>();
    private INode _root;

    public BehaviourTreeBuilder Selector(Action<BehaviourTreeBuilder> buildChildren = null)
        => BuildComposite<Selector>(buildChildren);

    public BehaviourTreeBuilder Sequence(Action<BehaviourTreeBuilder> buildChildren = null)
        => BuildComposite<Sequence>(buildChildren);

    public BehaviourTreeBuilder Condition(Func<bool> condition)
    {
        var node = new ConditionNode(condition);
        AddNode(node);
        return this;
    }

    public BehaviourTreeBuilder Action(Func<bool> action)
    {
        var node = new ActionNode(action);
        AddNode(node);
        return this;
    }

    public INode Build() => _root;

    private BehaviourTreeBuilder BuildComposite<T>(Action<BehaviourTreeBuilder> buildChildren)
        where T : CompositeNode, new()
    {
        var composite = new T();
        AddNode(composite);
        _compositeStack.Push(composite);
        buildChildren?.Invoke(this);
        _compositeStack.Pop();
        return this;
    }

    private void AddNode(INode node)
    {
        if (_compositeStack.Count == 0)
        {
            _root = node;
        }
        else
        {
            _compositeStack.Peek().AddChild(node);
        }
    }
}

public class RobotBehaviourTree
{
    private readonly WarehouseRobot _robot;

    public RobotBehaviourTree(WarehouseRobot robot) => _robot = robot;

    public INode BuildTree()
    {
        return new BehaviourTreeBuilder()
            // Корневой узел: Selector (выполняет первую успешную ветку)
            .Selector(root => root
                // Ветка 1: Доставка груза
                .Sequence(delivery => delivery
                    .Condition(_robot.IsAtTarget)         // Проверка прибытия в цель
                    .Condition(_robot.HasPackageInHand)   // Проверка наличия груза
                    .Action(_robot.DeliverPackage)        // Доставить груз
                )
                // Ветка 2: Навигация к цели
                .Sequence(navigation => navigation
                    .Condition(() => !_robot.IsAtTarget()) // Условие: робот НЕ в цели
                    .Selector(movement => movement          // Выбор стратегии движения
                                                            // Стратегия 1: Движение вперед
                        .Sequence(moveForward => moveForward
                            .Condition(() => _robot.IsFacingTarget() && !_robot.IsFacingObstacle())
                            .Action(_robot.MoveForward)
                        )
                        // Стратегия 2: Обход препятствий
                        .Sequence(avoidObstacle => avoidObstacle
                            .Condition(() => _robot.IsFacingObstacle())
                            .Selector(avoid => avoid
                                // Вариант 1: Повернуть налево + двигаться
                                .Sequence(turnAndMove => turnAndMove
                                    .Action(_robot.TurnLeft)
                                    .Action(_robot.MoveForward)
                                )
                                // Вариант 2: Повернуть направо + двигаться
                                .Sequence(turnAndMove => turnAndMove
                                    .Action(_robot.TurnRight)
                                    .Action(_robot.MoveForward)
                                )
                            )
                        )
                        // Стратегия 3: Повороты для коррекции направления
                        .Selector(turning => turning
                            // Поворот направо по условию
                            .Sequence(turnRight => turnRight
                                .Condition(_robot.ShouldTurnRight)
                                .Action(_robot.TurnRight)
                            )
                            // Поворот налево по условию
                            .Sequence(turnLeft => turnLeft
                                .Condition(_robot.ShouldTurnLeft)
                                .Action(_robot.TurnLeft)
                            )
                            // Запасной вариант: Безусловный поворот налево
                            .Sequence(turnAny => turnAny
                                .Action(_robot.TurnLeft)
                            )
                        )
                    )
                )
            )
            .Build();
    }
}

public enum Direction { North, East, South, West }

public class WarehouseRobot
{
    public int X { get; private set; }
    public int Y { get; private set; }
    private readonly int _targetX;
    private readonly int _targetY;
    public Direction Facing { get; private set; }
    public bool HasPackage { get; private set; }
    
    private readonly List<(int, int)> _obstacles;

    public WarehouseRobot(int startX, int startY, Direction startDir, 
                        int targetX, int targetY, List<(int, int)> obstacles)
    {
        X = startX;
        Y = startY;
        Facing = startDir;
        _targetX = targetX;
        _targetY = targetY;
        _obstacles = obstacles;
    }

    public bool ShouldTurnRight()
    {
        var dx = _targetX - X;
        var dy = _targetY - Y;
        if (dx == 0 && dy == 0) return false;

        // Приоритет: движение по основной оси
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            return Facing switch
            {
                Direction.North => dx > 0,
                Direction.South => dx < 0,
                _ => false
            };
        }
        else
        {
            return Facing switch
            {
                Direction.East => dy < 0,
                Direction.West => dy > 0,
                _ => false
            };
        }
    }

    public bool ShouldTurnLeft()
    {
        var dx = _targetX - X;
        var dy = _targetY - Y;
        if (dx == 0 && dy == 0) return false;

        // Приоритет: движение по основной оси
        if (Math.Abs(dx) >= Math.Abs(dy))
        {
            return Facing switch
            {
                Direction.North => dx < 0,
                Direction.South => dx > 0,
                _ => false
            };
        }
        else
        {
            return Facing switch
            {
                Direction.East => dy > 0,
                Direction.West => dy < 0,
                _ => false
            };
        }
    }

    public bool IsFacingObstacle()
    {
        var (dx, dy) = Facing switch
        {
            Direction.North => (0, 1),
            Direction.East => (1, 0),
            Direction.South => (0, -1),
            Direction.West => (-1, 0),
            _ => (0, 0)
        };
        return _obstacles.Contains((X + dx, Y + dy));
    }

    public bool PickPackage()
    {
        if (!HasPackage)
        {
            HasPackage = true;
            Console.WriteLine("Подобрал груз!");
            return true;
        }
        return false;
    }

    public bool DeliverPackage()
    {
        if (HasPackage && X == _targetX && Y == _targetY)
        {
            HasPackage = false;
            Console.WriteLine("Доставил груз!");
            return true;
        }
        return false;
    }

    public bool MoveForward()
    {
        var (dx, dy) = Facing switch
        {
            Direction.North => (0, 1),
            Direction.East => (1, 0),
            Direction.South => (0, -1),
            Direction.West => (-1, 0),
            _ => (0, 0)
        };

        return MoveIfPossible(X + dx, Y + dy);
    }

    public bool MoveBackward()
    {
        var (dx, dy) = Facing switch
        {
            Direction.North => (0, -1),
            Direction.East => (-1, 0),
            Direction.South => (0, 1),
            Direction.West => (1, 0),
            _ => (0, 0)
        };

        return MoveIfPossible(X + dx, Y + dy);
    }

    private bool MoveIfPossible(int newX, int newY)
    {
        if (_obstacles.Contains((newX, newY)))
        {
            Console.WriteLine($"Препятствие на [{newX},{newY}]");
            return false;
        }

        X = newX;
        Y = newY;
        Console.WriteLine($"Переместился на [{X},{Y}]");
        return true;
    }

    public bool TurnLeft()
    {
        Facing = (Direction)(((int)Facing + 3) % 4);
        Console.WriteLine($"Повернул налево, теперь смотрю {Facing}");
        return true;
    }

    public bool TurnRight()
    {
        Facing = (Direction)(((int)Facing + 1) % 4);
        Console.WriteLine($"Повернул направо, теперь смотрю {Facing}");
        return true;
    }

    public bool IsAtTarget() => X == _targetX && Y == _targetY;
    public bool HasPackageInHand() => HasPackage;

    public bool IsFacingTarget()
    {
        var dx = _targetX - X;
        var dy = _targetY - Y;

        if (dx == 0 && dy == 0) return true;

        // Разрешены диагональные движения к цели
        return Facing switch
        {
            Direction.North => dy > 0,
            Direction.East => dx > 0,
            Direction.South => dy < 0,
            Direction.West => dx < 0,
            _ => false
        };
    }

    public bool IsPackageDelivered() => !HasPackage && IsAtTarget();
}
