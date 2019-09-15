// An attribute that does nothing but can be used to mark a function
// as intending to be run on both the client and the server.
[System.AttributeUsage(System.AttributeTargets.Method)]
public class Everywhere : System.Attribute {}
