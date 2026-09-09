CREATE TYPE workflow_completion_actions AS ENUM ('Archive','Purge');
CREATE TYPE workflow_step_types AS ENUM ('Action','Delay','Suspended');
CREATE TYPE workflow_retry_types AS ENUM ('Timeout','Error');
CREATE TYPE workflow_step_result_status AS ENUM ('Success','Failure','Timeout');
CREATE TYPE performance_aggregation_types AS ENUM ('1Minute','5Minute','1Hour','1Day');

CREATE TABLE IF NOT EXISTS namespaces (
	id uuid PRIMARY KEY,
	name VARCHAR(512) NULL UNIQUE,
	enabled BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS workflows (
	id uuid,
	namespace_id uuid NOT NULL,
	name VARCHAR(512) NOT NULL UNIQUE,
	PRIMARY KEY (id, namespace_id),
	FOREIGN KEY (namespace_id) REFERENCES namespaces(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS activities (
	id uuid,
	namespace_id uuid NOT NULL,
	name VARCHAR(512) NOT NULL,
	PRIMARY KEY (id, namespace_id),
	FOREIGN KEY (namespace_id) REFERENCES namespaces(id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS archived_workflows (
	id uuid,
	workflow_id uuid,
	namespace_id uuid,
	scheduler_id uuid null,
	started_at TIMESTAMP NOT NULL,
	finished_at TIMESTAMP NOT NULL,
	is_successful BOOLEAN NOT NULL,
	error_message TEXT NULL,
	arguments JSONB NULL,
	PRIMARY KEY (id, namespace_id),
	FOREIGN KEY (workflow_id, namespace_id) REFERENCES workflows(id, namespace_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS archived_workflow_options(
	id uuid,
	namespace_id uuid,
	completion_action workflow_completion_actions NOT NULL,
	purge_delay VARCHAR(128) NULL,
	error_on_activity_timeout BOOLEAN NOT NULL DEFAULT FALSE,
	error_on_activity_failure BOOLEAN NOT NULL DEFAULT FALSE,
	PRIMARY KEY (id, namespace_id),
	FOREIGN KEY (id, namespace_id) REFERENCES archived_workflows(id, namespace_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS archived_workflow_metadata_entry(
	id uuid,
	namespace_id uuid,
	key_name VARCHAR(512),
	value_index INT,
	value TEXT NOT NULL,
	PRIMARY KEY (id, namespace_id, key_name, value_index),
	FOREIGN KEY (id, namespace_id) REFERENCES archived_workflows(id, namespace_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS archived_workflow_steps (
	id uuid,
	namespace_id uuid,
	step_id BIGINT NOT NULL,
	step_type workflow_step_types NOT NULL,
	step_index BIGINT NULL,
	step_name VARCHAR(512) NULL,
	start_time TIMESTAMP NOT NULL,
	end_time TIMESTAMP NOT NULL,
	input JSONB NULL,
	result_status workflow_step_result_status NULL,
	error_message TEXT NULL,
	result JSONB NULL,
	PRIMARY KEY (id, namespace_id, step_id),
	FOREIGN KEY (id, namespace_id) REFERENCES archived_workflows(id, namespace_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS archived_workflow_step_retries (
	id uuid,
	namespace_id uuid,
	step_id BIGINT NOT NULL,
	retry_index INT NOT NULL,
	retry_type workflow_retry_types NOT NULL,
	time_stamp TIMESTAMP NOT NULL,
	PRIMARY KEY (id, namespace_id, step_id, retry_index),
	FOREIGN KEY (id, namespace_id, step_id) REFERENCES archived_workflow_steps(id, namespace_id, step_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS workflow_performance (
    bucket_start timestamptz NOT NULL,
    workflow_id uuid NOT NULL,
	namespace_id uuid NOT NULL,
	aggregation_type performance_aggregation_types NOT NULL,
	started BIGINT NOT NULL,
	completed BIGINT NOT NULL,
	failed BIGINT NOT NULL,
	purged BIGINT NOT NULL,
    queue_latency_digest     bytea NOT NULL,
	PRIMARY KEY (bucket_start, workflow_id, namespace_id, aggregation_type),
    FOREIGN KEY (workflow_id, namespace_id)
        REFERENCES workflows(id, namespace_id)
);

CREATE TABLE IF NOT EXISTS activity_performance (
    bucket_start timestamptz NOT NULL,
    activity_id uuid NOT NULL,
	namespace_id uuid NOT NULL,
	aggregation_type performance_aggregation_types NOT NULL,
	started BIGINT NOT NULL,
	completed BIGINT NOT NULL,
	failed BIGINT NOT NULL,
	timed_out BIGINT NOT NULL,
	queue_latency_digest bytea NOT NULL,
    duration_digest bytea NOT NULL,
	PRIMARY KEY (bucket_start, activity_id, namespace_id, aggregation_type),
    FOREIGN KEY (activity_id, namespace_id)
        REFERENCES activities(id, namespace_id)
);

CREATE OR REPLACE PROCEDURE register_namespace(name varchar(512))
LANGUAGE plpgsql
AS $$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = name) THEN
		INSERT INTO namespaces (id, name) VALUES (gen_random_uuid(), name);
	ELSE 
		UPDATE namespaces SET enabled = TRUE WHERE name = name;
	END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE unregister_namespace(name varchar(512))
LANGUAGE plpgsql
AS $$
BEGIN
	UPDATE namespaces SET enabled = FALSE WHERE name = name;
END;
$$;

CREATE OR REPLACE PROCEDURE cleanup_performance_data()
LANGUAGE plpgsql
AS $$
BEGIN
	DELETE FROM workflow_performance WHERE 
		( aggregation_type = '1Minute' AND bucket_start < NOW() - INTERVAL '3 days' )
		OR ( aggregation_type = '5Minute' AND bucket_start < NOW() - INTERVAL '15 days' )
		OR ( aggregation_type = '1Hour' AND bucket_start < NOW() - INTERVAL '30 days' )
		OR ( aggregation_type = '1Day' AND bucket_start < NOW() - INTERVAL '365 days' );
	DELETE FROM activity_performance WHERE 
		( aggregation_type = '1Minute' AND bucket_start < NOW() - INTERVAL '3 days' )
		OR ( aggregation_type = '5Minute' AND bucket_start < NOW() - INTERVAL '15 days' )
		OR ( aggregation_type = '1Hour' AND bucket_start < NOW() - INTERVAL '30 days' )
		OR ( aggregation_type = '1Day' AND bucket_start < NOW() - INTERVAL '365 days' );
END;
$$;
