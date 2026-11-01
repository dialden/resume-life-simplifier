-- Preserve delivery intent recorded before messaging was introduced.
-- The command envelope matches the pinned MassTransit 8.5.12 bus outbox model.
WITH pending AS (
    SELECT delivery.posting_id, delivery.job_site_id,
           gen_random_uuid() AS outbox_id, gen_random_uuid() AS message_id
    FROM posting_deliveries delivery
    WHERE delivery.status = 'Pending'
      AND NOT EXISTS (
          SELECT 1 FROM messaging."OutboxMessage" queued
          WHERE queued."MessageType" =
                    'urn:message:DialDen.RLS.Infrastructure.Publishing:DeliverPostingToJobSite'
            AND queued."Body"::jsonb ->> 'postingId' = delivery.posting_id::text
            AND queued."Body"::jsonb ->> 'jobSiteId' = delivery.job_site_id
      )
      AND NOT EXISTS (
          SELECT 1 FROM transport.message queued
          JOIN transport.message_delivery destination
            ON destination.transport_message_id = queued.transport_message_id
          JOIN transport.queue queue ON queue.id = destination.queue_id
          WHERE queue.name = 'rls-job-site-publication' AND queue.type = 1
            AND queued.body ->> 'postingId' = delivery.posting_id::text
            AND queued.body ->> 'jobSiteId' = delivery.job_site_id
      )
),
states AS (
    INSERT INTO messaging."OutboxState" ("OutboxId", "LockId", "Created")
    SELECT outbox_id, gen_random_uuid(), now() FROM pending
    RETURNING "OutboxId"
)
INSERT INTO messaging."OutboxMessage" (
    "OutboxId", "MessageId", "SentTime", "ContentType", "MessageType", "Body", "DestinationAddress"
)
SELECT pending.outbox_id, pending.message_id, now(), 'application/json',
       'urn:message:DialDen.RLS.Infrastructure.Publishing:DeliverPostingToJobSite',
       jsonb_build_object('postingId', pending.posting_id, 'jobSiteId', pending.job_site_id)::text,
       'queue:rls-job-site-publication'
FROM pending
JOIN states ON states."OutboxId" = pending.outbox_id;
