ALTER TABLE postings
    ADD COLUMN status text NOT NULL DEFAULT 'Draft'
        CHECK (status IN ('Draft', 'Published'));
