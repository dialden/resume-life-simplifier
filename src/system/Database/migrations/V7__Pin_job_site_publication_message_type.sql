-- The command moved to DialDen.RLS.Messaging.Contracts with a pinned message URN.
-- Keep commands queued before the move readable by the worker.
UPDATE messaging."OutboxMessage"
SET "MessageType" = replace("MessageType",
        'urn:message:DialDen.RLS.Infrastructure.Publishing:PublishPostingOnJobSite',
        'urn:message:DialDen.RLS.Messaging:PublishPostingOnJobSite')
WHERE "MessageType" LIKE '%urn:message:DialDen.RLS.Infrastructure.Publishing:PublishPostingOnJobSite%';

UPDATE transport.message
SET message_type = replace(message_type,
        'urn:message:DialDen.RLS.Infrastructure.Publishing:PublishPostingOnJobSite',
        'urn:message:DialDen.RLS.Messaging:PublishPostingOnJobSite')
WHERE message_type LIKE '%urn:message:DialDen.RLS.Infrastructure.Publishing:PublishPostingOnJobSite%';
