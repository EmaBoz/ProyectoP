namespace CentroP.Api.Common.Messaging;

public sealed record EventMetadata(string EventId, string TraceId);

public sealed record EventReply(string InReplyTo);

public sealed record RequestEnvelope<TData>(EventMetadata Metadata, TData Data);

public sealed record ResponseEnvelope<TData>(EventMetadata Metadata, EventReply Reply, TData Data);
