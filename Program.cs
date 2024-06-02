Console.WriteLine("Hello, I'm am server, what is you're name?");
string? name = Console.ReadLine();
if (string.IsNullOrWhiteSpace(name))
{
    Console.WriteLine("You didn't enter a valid name!");
}
else
{
    Console.WriteLine("Hello " + name + ", welcome to the good side!");
}