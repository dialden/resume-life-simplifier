ALTER TABLE applications
    DROP CONSTRAINT applications_extraction_status_check,
    ADD CONSTRAINT applications_extraction_status_check
        CHECK (extraction_status IN ('NotProcessed', 'Pending', 'Completed', 'Failed'));
