-- Job sites registered by the worker from its plugins. Job sites it no longer has are kept,
-- unavailable, because publications still refer to them.
CREATE TABLE job_sites (
    id text PRIMARY KEY,
    name text NOT NULL,
    is_available boolean NOT NULL
);
