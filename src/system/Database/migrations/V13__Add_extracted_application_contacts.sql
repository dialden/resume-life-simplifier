ALTER TABLE applications
    DROP CONSTRAINT applications_extraction_status_check,
    ADD CONSTRAINT applications_extraction_status_check
        CHECK (extraction_status IN ('NotProcessed', 'Pending', 'Completed')),
    ADD COLUMN extracted_contacts jsonb NULL,
    ADD CONSTRAINT applications_extraction_result_check
        CHECK (
            (extraction_status = 'Completed'
                AND extracted_contacts IS NOT NULL
                AND jsonb_typeof(extracted_contacts) = 'object')
            OR (extraction_status <> 'Completed' AND extracted_contacts IS NULL)
        );
