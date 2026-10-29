CREATE TABLE posting_deliveries (
    posting_id integer NOT NULL REFERENCES postings (id) ON DELETE CASCADE,
    job_site_id text NOT NULL,
    status text NOT NULL DEFAULT 'Pending'
        CHECK (status IN ('Pending', 'Succeeded', 'Failed')),
    PRIMARY KEY (posting_id, job_site_id)
);
