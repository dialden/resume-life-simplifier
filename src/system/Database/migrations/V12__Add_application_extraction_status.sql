ALTER TABLE applications
    ADD COLUMN extraction_status text NOT NULL DEFAULT 'NotProcessed'
        CHECK (extraction_status IN ('NotProcessed', 'Pending'));

ALTER TABLE applications ALTER COLUMN extraction_status DROP DEFAULT;
