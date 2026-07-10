namespace RestApi.Authentication.Internal;

/// <summary>
/// A no-op <see cref="DelegatingHandler"/> used in gateway (session) mode, where authentication is
/// handled entirely by the Client Portal Gateway. It lets the DI pipeline register an auth handler
/// slot unconditionally and only swap in <see cref="OAuth1aSigningHandler"/> when OAuth is configured.
/// </summary>
internal sealed class PassThroughHandler : DelegatingHandler;
