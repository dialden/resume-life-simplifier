ALTER TABLE applications RENAME COLUMN extraction_status TO status;
ALTER TABLE applications DROP CONSTRAINT applications_extraction_status_check;
ALTER TABLE applications DROP CONSTRAINT applications_extraction_result_check;

UPDATE applications AS application SET status = CASE application.status
    WHEN 'NotProcessed' THEN 'NotProcessed'
    WHEN 'Pending' THEN 'AwaitingExtraction'
    WHEN 'Failed' THEN 'ExtractionFailed'
    WHEN 'Completed' THEN CASE WHEN posting.required_education IS NULL
        THEN 'Ready' ELSE 'AwaitingComparison' END
END
FROM postings posting
WHERE posting.id = application.posting_id;

ALTER TABLE applications
    ADD CONSTRAINT applications_status_check CHECK (status IN
        ('NotProcessed', 'AwaitingExtraction', 'ExtractionFailed', 'AwaitingComparison', 'ComparisonFailed', 'Ready')),
    ADD CONSTRAINT applications_extraction_result_check CHECK (
        status IN ('AwaitingComparison', 'ComparisonFailed', 'Ready')
        OR (extracted_name IS NULL AND extracted_email IS NULL AND extracted_phone_number IS NULL
            AND extracted_postal_address IS NULL AND extracted_linkedin_url IS NULL)),
    ADD COLUMN education_original_met boolean NULL,
    ADD COLUMN education_confidence double precision NULL CHECK (education_confidence BETWEEN 0 AND 1),
    ADD COLUMN education_evidence text NULL,
    ADD COLUMN education_override_met boolean NULL,
    ADD COLUMN education_note text NULL,
    ADD COLUMN review_version integer NOT NULL DEFAULT 0;

WITH pending AS (
    SELECT application.id AS application_id, application.posting_id,
           gen_random_uuid() AS outbox_id, gen_random_uuid() AS message_id
    FROM applications application WHERE application.status = 'AwaitingComparison'
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
       'urn:message:DialDen.RLS.Messaging.Contracts:CompareApplication',
       jsonb_build_object('postingId', pending.posting_id, 'applicationId', pending.application_id)::text,
       'queue:rls-application-comparison'
FROM pending JOIN states ON states."OutboxId" = pending.outbox_id;
