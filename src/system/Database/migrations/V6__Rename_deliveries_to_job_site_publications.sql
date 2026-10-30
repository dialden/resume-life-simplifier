ALTER TABLE posting_deliveries RENAME TO job_site_publications;
ALTER TABLE job_site_publications RENAME CONSTRAINT posting_deliveries_pkey TO job_site_publications_pkey;
ALTER TABLE job_site_publications
    RENAME CONSTRAINT posting_deliveries_posting_id_fkey TO job_site_publications_posting_id_fkey;
ALTER TABLE job_site_publications
    RENAME CONSTRAINT posting_deliveries_status_check TO job_site_publications_status_check;

-- Keep commands queued before the rename readable by the renamed message contract.
UPDATE messaging."OutboxMessage"
SET "MessageType" = replace("MessageType",
        'urn:message:DialDen.RLS.Infrastructure.Publishing:DeliverPostingToJobSite',
        'urn:message:DialDen.RLS.Infrastructure.Publishing:PublishPostingOnJobSite')
WHERE "MessageType" LIKE '%:DeliverPostingToJobSite%';

UPDATE transport.message
SET message_type = replace(message_type,
        'urn:message:DialDen.RLS.Infrastructure.Publishing:DeliverPostingToJobSite',
        'urn:message:DialDen.RLS.Infrastructure.Publishing:PublishPostingOnJobSite')
WHERE message_type LIKE '%:DeliverPostingToJobSite%';
