# Patterns We DON'T Use (Never Suggest)
- AutoMapper (write explicit mappings)
- Exceptions for business logic errors
- Stored procedures
- Comments inside non API public code

## Git Workflow
- Branch naming: `feature/`, `bugfix/`, `hotfix/`
- Commit format: `type: description` (feat, fix, refactor, test, docs)
- Always create a branch before changes
- Run tests before committing

## Commands
- Build: `dotnet build`
- Test: `dotnet test`
- Run App: `dotnet run --project src/Phoenix/Host`
- Format: `dotnet format`

# Code Conventions

## Overview

This defines coding conventions that apply to all .NET projects being developed. The goal is to actively leverage the latest features available since .NET 8 (C# 12/13/14, .NET 8/9/10) to produce code with high readability, maintainability, and performance.

## Core Principles

- Actively use the latest features available since .NET 8 (C# 12/13/14, .NET 8/9/10)
- Always prefer the latest features unless backward compatibility is required
- Avoid outdated language constructs
- **IMPORTANT: Underscore prefixes (`_field`) are strictly prohibited**
- Follow Microsoft's official coding conventions
- Maintain consistency and apply the same style across the entire team

### Latest .NET/C# Features (By Version)

Actively leverage the key features introduced in each version since .NET 8. Always prefer the latest features unless backward compatibility is required.

### Primary Constructors

- Define parameters in class or struct declarations and use them throughout the class
- Reduces explicit field declarations and simplifies initialization

Good example:

```csharp
public class Person(string name, int age)
{
    public string Name => name;
    public int Age => age;

    public void Display()
    {
        Console.WriteLine($"{name} is {age} years old");
    }
}
```

Bad example:

```csharp
public class Person
{
    private string name;
    private int age;

    public Person(string name, int age)
    {
        this.name = name;
        this.age = age;
    }

    public string Name => name;
    public int Age => age;
}
```

### Collection Expressions

- Create collections concisely using bracket syntax and the spread operator
- Useful when combining multiple collections

Good example:

```csharp
int[] array = [1, 2, 3, 4, 5];
List<string> list = ["one", "two", "three"];

int[] row0 = [1, 2, 3];
int[] row1 = [4, 5, 6];

// Combine with spread operator
int[] combined = [..row0, ..row1];
```

### Default Lambda Parameters

- Specify default parameter values in lambda expressions

Good example:

```csharp
var incrementBy = (int source, int increment = 1) => source + increment;

Console.WriteLine(incrementBy(5));
Console.WriteLine(incrementBy(5, 3));
```

### Alias Any Type

- Assign aliases to complex types using the `using` directive

Good example:

```csharp
using Point = (int x, int y);
using ProductList = System.Collections.Generic.List<(string Name, decimal Price)>;

Point origin = (0, 0);
ProductList products = [("Product1", 100m), ("Product2", 200m)];
```

### Params Collections

- The `params` modifier can now be used with collection types beyond arrays
- Works with `List<T>`, `Span<T>`, `ReadOnlySpan<T>`, `IEnumerable<T>`, etc.

Good example:

```csharp
public void ProcessItems(params List<string> items)
{
    foreach (var item in items)
    {
        Console.WriteLine(item);
    }
}

// When memory efficiency matters
public void ProcessData(params ReadOnlySpan<int> data)
{
    foreach (var value in data)
    {
        Process(value);
    }
}
```

### New Lock Type

- Use `System.Threading.Lock` type for faster thread synchronization
- Faster than the traditional `Monitor`-based locking

Good example:

```csharp
private readonly Lock lockObject = new();

public void UpdateData()
{
    lock (lockObject)
    {
        // Critical section
    }
}
```

Bad example:

```csharp
// Traditional object-based locking (not recommended in C# 13)
private readonly object lockObject = new();

public void UpdateData()
{
    lock (lockObject)
    {
        // Critical section
    }
}
```

### Partial Properties and Indexers

- Partial properties and indexers are now supported
- Allows separating definition from implementation

Good example:

```csharp
// Definition part
public partial class DataModel
{
    public partial string Name { get; set; }
}

// Implementation part
public partial class DataModel
{
    private string name;

    public partial string Name
    {
        get => name;
        set => name = value ?? throw new ArgumentNullException(nameof(value));
    }
}
```

### Implicit Index Access

- The `^` operator can now be used in object initializers

Good example:

```csharp
var countdown = new TimerBuffer
{
    buffer =
    {
        [^1] = 0,
        [^2] = 1,
        [^3] = 2
    }
};
```

### Ref Struct Enhancements

- `ref struct` types can now implement interfaces
- `ref struct` can be used in generic types (with the `allows ref struct` constraint)

Good example:

```csharp
public ref struct SpanWrapper<T> : IEnumerable<T>
{
    private Span<T> span;

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var item in span)
        {
            yield return item;
        }
    }
}
```

### Extension Members

- Use Extension Members to implement clean API extensions
- Add functionality without polluting the original type

Good example:

```csharp
extension<TSource>(IEnumerable<TSource> source)
{
    public bool IsEmpty => !source.Any();
    public int Count => source.Count();
}
```

### Field-Backed Properties

- Use the `field` keyword to eliminate explicit backing fields
- Write validation logic concisely
- **Explicit backing fields with underscore prefixes are strictly prohibited**

Good example:

```csharp
// Using C# 14's field keyword
public string Name
{
    get;
    set => field = value ?? throw new ArgumentNullException(nameof(value));
}

// When an explicit backing field is unavoidable, no underscore
private string name;

public string Name
{
    get => name;
    set => name = value ?? throw new ArgumentNullException(nameof(value));
}
```

Bad example:

```csharp
// Underscore prefix is strictly prohibited
private string _name;

public string Name
{
    get => _name;
    set => _name = value ?? throw new ArgumentNullException(nameof(value));
}
```

### Null-Conditional Assignment

- Use `?.` to write null checks concisely
- Reduces redundant null checks

Good example:

```csharp
customer?.Order = GetCurrentOrder();
```

Bad example:

```csharp
if (customer != null)
{
    customer.Order = GetCurrentOrder();
}
```

### Implicit Span Conversions

- Use `Span<T>` and `ReadOnlySpan<T>` in performance-critical code
- Leverage automatic conversions between array and span types

## Naming Conventions

### Pascal Casing

- Type names (class, record, struct, interface, enum)
- Public members (properties, methods, events)
- Namespaces

Good example:

```csharp
public class CustomerOrder
{
    public string OrderId { get; set; }
    public void ProcessOrder() { }
}
```

### Camel Casing

- Local variables
- Method parameters
- Private fields (**never use underscore prefixes**)

Good example:

```csharp
public class OrderProcessor
{
    // No underscore
    private string customerName;

    // No underscore
    private int orderCount;

    public void ProcessOrder(string orderId)
    {
        var customerName = GetCustomerName(orderId);
        string processedResult = Process(customerName);
    }
}
```

Bad example:

```csharp
public class OrderProcessor
{
    // Underscore prefix is strictly prohibited
    private string _customerName;

    // Underscore prefix is strictly prohibited
    private int _orderCount;
}
```

### Interface Naming

- Use the `I` prefix

Good example:

```csharp
public interface IOrderProcessor
{
    void Process(Order order);
}
```

### Type Parameter Naming

- Use the `T` prefix
- Use meaningful names

Good example:

```csharp
public class Repository<TEntity> where TEntity : class
{
    public void Add(TEntity entity) { }
}
```

## Code Layout

### Indentation

- Use 4 spaces
- Do not use tabs

### Curly Braces

- Allman style (opening and closing braces on separate lines)

Good example:

```csharp
public void ProcessOrder(Order order)
{
    if (order != null)
    {
        order.Process();
    }
}

// Always use braces even for single lines
if (isValid)
{
    Execute();
}

for (int i = 0; i < 10; i++)
{
    Process(i);
}
```

### Line Statements

- Write only one statement per line
- Write only one declaration per line
- Add one blank line between method definitions and property definitions

Good example:

```csharp
public class Order
{
    public string OrderId { get; set; }

    public void Process()
    {
        var result = Validate();
        Execute(result);
    }

    private bool Validate()
    {
        return OrderId != null;
    }
}
```

### Namespaces

- Use file-scoped namespaces

Good example:

```csharp
namespace YourProject.Orders;

public class OrderProcessor
{
    // Implementation
}
```

Bad example:

```csharp
namespace YourProject.Orders
{
    public class OrderProcessor
    {
        // Implementation
    }
}
```

### Using Directives

- Place outside namespace declarations
- Sort alphabetically

Good example:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

namespace YourProject.Orders;
```

## Types and Variables

### Type Specification

- Use language keywords (`string`, `int`, `bool`)
- Do not use runtime types (`System.String`, `System.Int32`)

Good example:

```csharp
string name = "John";
int count = 10;
bool isValid = true;
```

Bad example:

```csharp
String name = "John";
Int32 count = 10;
Boolean isValid = true;
```

### Type Inference (var)

- Use `var` only when the type is obvious from the assigned value
- Explicitly specify built-in types

Good example:

```csharp
// Obvious
var orders = new List<Order>();

// Obvious
var customer = GetCustomer();

// Explicit for built-in types
int count = 10;

// Explicit for built-in types
string name = "John";
```

Bad example:

```csharp
// Avoid var for built-in types
var count = 10;

// Avoid var for built-in types
var name = "John";
```

## Strings

### String Interpolation

- Use string interpolation for short string concatenation

Good example:

```csharp
string message = $"Order {orderId} processed successfully";
```

Bad example:

```csharp
string message = "Order " + orderId + " processed successfully";
```

### StringBuilder

- Use `StringBuilder` when appending large amounts of text in a loop

Good example:

```csharp
var builder = new StringBuilder();
for (int i = 0; i < 1000; i++)
{
    builder.Append($"Line {i}\n");
}
```

### Raw String Literals

- Prefer Raw String Literals over escape sequences

Good example:

```csharp
string json = """
{
    "name": "John",
    "age": 30
}
""";
```

## Collections and Object Initialization

### Collection Initialization

- Use Collection Expressions (see section above)

### Object Initializers

- Use object initializers to simplify creation

Good example:

```csharp
var customer = new Customer
{
    Name = "John",
    Email = "john@example.com"
};
```

## Exception Handling

### Catch Specific Exceptions

- Catch specific exceptions instead of the general `System.Exception`

Good example:

```csharp
try
{
    ProcessOrder(order);
}
catch (ArgumentNullException ex)
{
    Logger.Error("Order is null", ex);
}
```

Bad example:

```csharp
try
{
    ProcessOrder(order);
}
catch (Exception ex) // Too general
{
    Logger.Error("Error", ex);
}
```

### Using Statements

- Use `using` statements instead of try-finally

Good example:

```csharp
using var connection = new SqlConnection(connectionString);
connection.Open();
// Process
```

Bad example:

```csharp
SqlConnection connection = null;
try
{
    connection = new SqlConnection(connectionString);
    connection.Open();
    // Process
}
finally
{
    connection?.Dispose();
}
```

## LINQ

### Meaningful Variable Names

- Use meaningful names for query variables

Good example:

```csharp
var activeCustomers = from customer in customers
                      where customer.IsActive
                      select customer;
```

### Early Filtering

- Use `where` clauses to filter data early

Good example:

```csharp
var result = customers
    .Where(c => c.IsActive)
    .Select(c => c.Name)
    .ToList();
```

### Implicit Typing

- Use implicit typing in LINQ declarations

Good example:

```csharp
var query = from customer in customers
            where customer.IsActive
            select customer;
```

## Lambda Expressions

### Event Handlers

- Use lambda expressions for handlers that don't need to be removed

Good example:

```csharp
button.Click += (s, e) => ProcessClick();
```

### Parameter Modifiers

- Leverage C# 14 features to use modifiers while maintaining type inference

Good example:

```csharp
TryParse<int> parse = (text, out result) => int.TryParse(text, out result);
```

## Comments

### Single-Line Comments

- Use `//` for brief descriptions
- Add one space after the comment delimiter
- **Comments must always be on their own line (never on the same line as code)**
- Add one blank line before comments

Good example:

```csharp
// Process the customer order
ProcessOrder(order);

var processor = new OrderProcessor();

// Execute the order
var result = processor.ProcessOrder(order);
```

Bad example:

```csharp
ProcessOrder(order); // Process the customer order (same line as code is prohibited)

var processor = new OrderProcessor();
// No blank line before this comment (bad example)
var result = processor.ProcessOrder(order);
```

## Static Members

### Call Via Class Name

- Call static members through the class name

Good example:

```csharp
var result = OrderProcessor.ProcessOrder(order);
```

Bad example:

```csharp
var processor = new OrderProcessor();

// Calling a static method through an instance is misleading
var result = processor.ProcessOrder(order);
```

## Checklist

### Before Writing Code

- [ ] Familiar with the latest .NET/C# features (C# 12/13/14)
- [ ] Project target framework is set to .NET 8 or later
- [ ] Understand the naming conventions

### While Writing Code

**Mandatory Rules:**

- [ ] **No underscore prefixes used**
- [ ] **Comments are always on their own line (never on the same line as code)**
- [ ] **One blank line before comments**

**C# 12+ Features:**

- [ ] Using Primary Constructors (where applicable)
- [ ] Using Collection Expressions
- [ ] Leveraging Default Lambda Parameters (where applicable)
- [ ] Using Alias Any Type for complex types (where applicable)

**C# 13+ Features:**

- [ ] Using Params Collections (where applicable)
- [ ] Using New Lock Type (when thread synchronization is needed)
- [ ] Leveraging Partial Properties and Indexers (where applicable)
- [ ] Using Implicit Index Access in object initializers (where applicable)

**C# 14+ Features:**

- [ ] Using `field` keyword for concise backing fields
- [ ] Leveraging Extension Members (where applicable)
- [ ] Using Null-Conditional Assignment
- [ ] Using Lambda Parameters with Modifiers (where applicable)

**Basic Rules:**

- [ ] Using file-scoped namespaces
- [ ] Using language keywords (`string`, `int`)
- [ ] Using `var` appropriately (only when the type is obvious)
- [ ] Using string interpolation
- [ ] Using Raw String Literals (where applicable)
- [ ] Using Object Initializers
- [ ] Using `using` statements
- [ ] Catching specific exceptions
- [ ] Applying early filtering in LINQ expressions
- [ ] Using meaningful variable names
- [ ] Comments are concise and clear
- [ ] XML documentation for public members
- [ ] Curly braces placed in Allman style
- [ ] Using 4-space indentation

### After Writing Code

- [ ] Code is written in a consistent style
- [ ] Leveraging the latest features since .NET 8 (C# 12/13/14)
- [ ] Following naming conventions
- [ ] Code is highly readable and easy to maintain