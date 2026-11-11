
DO $$ 
BEGIN
    CREATE TYPE "performance_aggregation_types" AS ENUM ('OneMinute', 'FiveMinute', 'OneHour', 'OneDay');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DO $$ 
BEGIN
    CREATE TYPE "workflow_completion_actions" AS ENUM ('Archive', 'Purge');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DO $$ 
BEGIN
    CREATE TYPE "workflow_retry_types" AS ENUM ('Timeout', 'Error');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

DO $$ 
BEGIN
    CREATE TYPE "workflow_step_result_status" AS ENUM ('Success', 'Failure', 'Timeout');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;


DO $$ 
BEGIN
    CREATE TYPE "workflow_step_types" AS ENUM ('Action', 'Delay', 'Suspended', 'ChildWorkflow');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

CREATE TABLE IF NOT EXISTS "namespaces" (
    "id" uuid NOT NULL,
    "name" character varying(2048) UNIQUE,
    "enabled" boolean DEFAULT true NOT NULL,
    PRIMARY KEY ("id")
);

CREATE TABLE IF NOT EXISTS "activities" (
    "id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "name" character varying(2048) NOT NULL,
    "argument_definition" jsonb,
    PRIMARY KEY ("id", "namespace_id"),
    FOREIGN KEY ("namespace_id") REFERENCES "namespaces" ("id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "workflows" (
    "id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "name" character varying(2048) NOT NULL,
    "argument_definition" jsonb,
    PRIMARY KEY ("id", "namespace_id"),
    FOREIGN KEY ("namespace_id") REFERENCES "namespaces" ("id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "archived_workflows" (
    "id" uuid NOT NULL,
    "workflow_id" uuid,
    "namespace_id" uuid NOT NULL,
    "scheduler_id" uuid,
    "started_at" timestamp NOT NULL,
    "finished_at" timestamp NOT NULL,
    "is_successful" boolean NOT NULL,
    "error_message" text,
    "arguments" jsonb,
    PRIMARY KEY ("id", "workflow_id", "namespace_id"),
    FOREIGN KEY ("workflow_id", "namespace_id") REFERENCES "workflows" ("id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "archived_workflow_metadata_entry" (
    "id" uuid NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "key_name" character varying(2048) NOT NULL,
    "value_index" integer NOT NULL,
    "value" text NOT NULL,
    PRIMARY KEY ("id", "workflow_id", "namespace_id", "key_name", "value_index"),
    FOREIGN KEY ("id", "workflow_id", "namespace_id") REFERENCES "archived_workflows" ("id", "workflow_id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "archived_workflow_options" (
    "id" uuid NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "completion_action" workflow_completion_actions NOT NULL,
    "purge_delay" character varying(128),
    "error_on_activity_timeout" boolean DEFAULT false NOT NULL,
    "error_on_activity_failure" boolean DEFAULT false NOT NULL,
    PRIMARY KEY ("id", "workflow_id", "namespace_id"),
    FOREIGN KEY ("id", "workflow_id", "namespace_id") REFERENCES "archived_workflows" ("id", "workflow_id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "archived_workflow_steps" (
    "id" uuid NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "step_id" int NOT NULL,
    "step_type" workflow_step_types NOT NULL,
    "step_index" bigint,
    "activity_id" uuid,
    "start_time" timestamp NOT NULL,
    "end_time" timestamp NOT NULL,
    "input" jsonb,
    "result_status" workflow_step_result_status,
    "error_message" text,
    "result" jsonb,
	"child_workflow_instance_id" uuid,
	"child_workflow_id" uuid,
    PRIMARY KEY ("id", "workflow_id", "namespace_id", "step_id"),
    FOREIGN KEY ("id", "workflow_id", "namespace_id") REFERENCES "archived_workflows" ("id", "workflow_id", "namespace_id") ON DELETE CASCADE,
    FOREIGN KEY ("activity_id", "namespace_id") REFERENCES "activities" ("id", "namespace_id") ON DELETE CASCADE,
	FOREIGN KEY ("child_workflow_instance_id", "child_workflow_id", "namespace_id") REFERENCES "archived_workflows" ("id", "workflow_id", "namespace_id") ON DELETE CASCADE,
	FOREIGN KEY ("child_workflow_id", "namespace_id") REFERENCES "workflows" ("id", "namespace_id") ON DELETE CASCADE
);


CREATE TABLE IF NOT EXISTS "archived_workflow_step_retries" (
    "id" uuid NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "step_id" int NOT NULL,
    "retry_index" integer NOT NULL,
    "retry_type" workflow_retry_types NOT NULL,
    "time_stamp" timestamp NOT NULL,
    PRIMARY KEY ("id", "workflow_id", "namespace_id", "step_id", "retry_index"),
    FOREIGN KEY ("id", "workflow_id", "namespace_id", "step_id") REFERENCES "archived_workflow_steps" ("id", "workflow_id", "namespace_id", "step_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "activity_performance_raw" (
    "id" uuid NOT NULL,
    "window" timestamptz NOT NULL,
    "activity_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "started" bigint NOT NULL,
    "completed" bigint NOT NULL,
    "failed" bigint NOT NULL,
    "timed_out" bigint NOT NULL,
    "queue_latencies" jsonb NOT NULL,
    "durations" jsonb NOT NULL,
    "processed" boolean DEFAULT false NOT NULL,
    PRIMARY KEY ("id"),
    FOREIGN KEY ("activity_id", "namespace_id") REFERENCES "activities" ("id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "activity_performance" (
    "bucket_start" timestamptz NOT NULL,
    "activity_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "aggregation_type" performance_aggregation_types NOT NULL,
    "started" bigint NOT NULL,
    "completed" bigint NOT NULL,
    "failed" bigint NOT NULL,
    "timed_out" bigint NOT NULL,
    "queue_latency_digest" bytea NOT NULL,
    "duration_digest" bytea NOT NULL,
    PRIMARY KEY ("bucket_start", "activity_id", "namespace_id", "aggregation_type"),
    FOREIGN KEY ("activity_id", "namespace_id") REFERENCES "activities" ("id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "workflow_performance_raw" (
    "id" uuid NOT NULL,
    "window" timestamptz NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "started" bigint NOT NULL,
    "completed" bigint NOT NULL,
    "failed" bigint NOT NULL,
    "purged" bigint NOT NULL,
    "queue_latencies" jsonb NOT NULL,
    "processed" boolean DEFAULT false NOT NULL,
    PRIMARY KEY ("id"),
    FOREIGN KEY ("workflow_id", "namespace_id") REFERENCES "workflows" ("id", "namespace_id") ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS "workflow_performance" (
    "bucket_start" timestamptz NOT NULL,
    "workflow_id" uuid NOT NULL,
    "namespace_id" uuid NOT NULL,
    "aggregation_type" performance_aggregation_types NOT NULL,
    "started" bigint NOT NULL,
    "completed" bigint NOT NULL,
    "failed" bigint NOT NULL,
    "purged" bigint NOT NULL,
    "queue_latency_digest" bytea NOT NULL,
    PRIMARY KEY ("bucket_start", "workflow_id", "namespace_id", "aggregation_type"),
    FOREIGN KEY ("workflow_id", "namespace_id") REFERENCES "workflows" ("id", "namespace_id") ON DELETE CASCADE
);

CREATE OR REPLACE PROCEDURE "cleanup_performance_data" () 
LANGUAGE plpgsql 
AS $$
BEGIN
	DELETE FROM workflow_performance WHERE 
		( aggregation_type = 'OneMinute' AND bucket_start < NOW() - INTERVAL '3 days' )
		OR ( aggregation_type = 'FiveMinute' AND bucket_start < NOW() - INTERVAL '15 days' )
		OR ( aggregation_type = 'OneHour' AND bucket_start < NOW() - INTERVAL '30 days' )
		OR ( aggregation_type = 'OneDay' AND bucket_start < NOW() - INTERVAL '365 days' );
	DELETE FROM activity_performance WHERE 
		( aggregation_type = 'OneMinute' AND bucket_start < NOW() - INTERVAL '3 days' )
		OR ( aggregation_type = 'FiveMinute' AND bucket_start < NOW() - INTERVAL '15 days' )
		OR ( aggregation_type = 'OneHour' AND bucket_start < NOW() - INTERVAL '30 days' )
		OR ( aggregation_type = 'OneDay' AND bucket_start < NOW() - INTERVAL '365 days' );
END;
$$;

CREATE OR REPLACE PROCEDURE "register_namespace" (IN p_name character varying)
LANGUAGE plpgsql 
AS $$
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_name ) THEN
		INSERT INTO namespaces (id, name, enabled) VALUES (gen_random_uuid(), p_name , TRUE);
	ELSE 
		UPDATE namespaces SET enabled = TRUE WHERE name = p_name;
	END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "unregister_namespace" (IN p_name character varying)
LANGUAGE plpgsql 
AS $$
BEGIN
	UPDATE namespaces SET enabled = FALSE WHERE name = p_name;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_activity_performance_entry" (
    IN p_entry_window timestamptz, 
    IN p_namespace character varying ,
    IN p_activity character varying ,
    IN p_started bigint,
    IN p_completed bigint,
    IN p_failed bigint,
    IN p_timed_out bigint,
    IN p_queue_latencies jsonb,
    IN p_durations jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_activity_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM activities WHERE name = p_activity AND namespace_id = v_namespace_id) THEN
        v_activity_id := gen_random_uuid();
        INSERT INTO activities (id, namespace_id, name) VALUES (v_activity_id, v_namespace_id, p_activity);
    ELSE
        SELECT id INTO v_activity_id FROM activities WHERE name = p_activity AND namespace_id = v_namespace_id;
    END IF;
    INSERT INTO activity_performance_raw (id, "window", namespace_id, activity_id, started, completed, failed, timed_out, queue_latencies, durations) 
    VALUES (gen_random_uuid(), p_entry_window, v_namespace_id, v_activity_id, p_started, p_completed, p_failed, p_timed_out, p_queue_latencies, p_durations);
END;
$$;

CREATE OR REPLACE PROCEDURE "add_workflow_performance_entry" (
    IN p_entry_window timestamptz,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_started bigint,
    IN p_completed bigint,
    IN p_failed bigint,
    IN p_purged bigint,
    IN p_queue_latencies jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    INSERT INTO workflow_performance_raw (id, "window", namespace_id, workflow_id, started, completed, failed, purged, queue_latencies) 
    VALUES (gen_random_uuid(), p_entry_window, v_namespace_id, v_workflow_id, p_started, p_completed, p_failed, p_purged, p_queue_latencies);
END;
$$;

CREATE OR REPLACE PROCEDURE "create_archived_workflow" (
    IN p_id uuid ,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_scheduler_id uuid,
    IN p_started_at timestamp ,
    IN p_finished_at timestamp ,
    IN p_is_successful boolean ,
    IN p_error_message text,
    IN p_arguments jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflows WHERE id = p_id AND workflow_id = v_workflow_id AND namespace_id = v_namespace_id) THEN
        INSERT INTO archived_workflows (id, workflow_id, namespace_id, scheduler_id, started_at, finished_at, is_successful, error_message, arguments) 
        VALUES (p_id, v_workflow_id, v_namespace_id, p_scheduler_id, p_started_at, p_finished_at, p_is_successful, p_error_message, p_arguments);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "set_archived_workflow_options" (
    IN p_id uuid ,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_completion_action workflow_completion_actions ,
    IN p_purge_delay character varying(128),
    IN p_error_on_activity_timeout boolean ,
    IN p_error_on_activity_failure boolean 
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_options WHERE id = p_id AND workflow_id = v_workflow_id AND namespace_id = v_namespace_id) THEN
        INSERT INTO archived_workflow_options (id, workflow_id, namespace_id, completion_action, purge_delay, error_on_activity_timeout, error_on_activity_failure) 
        VALUES (p_id, v_workflow_id, v_namespace_id, p_completion_action, p_purge_delay, p_error_on_activity_timeout, p_error_on_activity_failure);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_archived_workflow_metadata_entry" (
    IN p_id uuid ,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_key_name character varying(2048) ,
    IN p_value_index integer ,
    IN p_value text 
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_metadata_entry WHERE id = p_id AND workflow_id = v_workflow_id AND namespace_id = v_namespace_id AND key_name = p_key_name AND value_index = p_value_index) THEN
        INSERT INTO archived_workflow_metadata_entry (id, workflow_id, namespace_id, key_name, value_index, value) 
        VALUES (p_id, v_workflow_id, v_namespace_id, p_key_name, p_value_index, p_value);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_archived_workflow_step" (
    IN p_id uuid ,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_step_id int ,
    IN p_step_type workflow_step_types ,
    IN p_step_index bigint,
    IN p_step_name character varying(2048),
    IN p_start_time timestamp ,
    IN p_end_time timestamp ,
    IN p_input jsonb,
    IN p_result_status workflow_step_result_status,
    IN p_error_message text,
    IN p_result jsonb,
    IN p_child_workflow_instance_id uuid,
    in p_child_workflow_name character varying
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
    v_activity_id uuid;
    v_child_workflow_instance_id uuid;
    v_child_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    IF p_step_name IS NOT NULL THEN
        IF NOT EXISTS (SELECT 1 FROM activities WHERE name = p_step_name AND namespace_id = v_namespace_id) THEN
            v_activity_id := gen_random_uuid();
            INSERT INTO activities (id, namespace_id, name) VALUES (v_activity_id, v_namespace_id, p_step_name);
        ELSE
            SELECT id INTO v_activity_id FROM activities WHERE name = p_step_name AND namespace_id = v_namespace_id;
        END IF;
    ELSE
        v_activity_id := NULL;
    END IF;
    v_child_workflow_instance_id := null;
    v_child_workflow_id := null;
    IF p_child_workflow_instance_id IS NOT NULL THEN 
        IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_child_workflow_name AND namespace_id = v_namespace_id) THEN
            v_child_workflow_id := gen_random_uuid();
            INSERT INTO workflows (id, namespace_id, name) VALUES (v_child_workflow_id, v_namespace_id, p_child_workflow_name);
        ELSE
            SELECT id INTO v_child_workflow_id FROM workflows WHERE name = p_child_workflow_name AND namespace_id = v_namespace_id;
        END IF;
        IF EXISTS (SELECT 1 FROM archived_workflow_steps WHERE id = v_child_workflow_instance_id AND workflow_id = v_child_workflow_id AND namespace_id = v_namespace_id) THEN
            v_child_workflow_instance_id := p_child_workflow_instance_id;
        END IF;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_steps WHERE id = p_id AND workflow_id = v_workflow_id AND namespace_id = v_namespace_id AND step_id = p_step_id) THEN
        INSERT INTO archived_workflow_steps (id, workflow_id, namespace_id, step_id, step_type, step_index, activity_id, start_time, end_time, input, result_status, error_message, result, child_workflow_instance_id, child_workflow_id) 
        VALUES (p_id, v_workflow_id, v_namespace_id, p_step_id, p_step_type, p_step_index, v_activity_id, p_start_time, p_end_time, p_input, p_result_status, p_error_message, p_result, v_child_workflow_instance_id,  v_child_workflow_id);
    END IF;
END;
$$;

CREATE OR REPLACE PROCEDURE "add_archived_workflow_step_retry" (
    IN p_id uuid ,
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_step_id bigint ,
    IN p_retry_index integer ,
    IN p_retry_type workflow_retry_types ,
    IN p_time_stamp timestamp 
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    IF NOT EXISTS (SELECT 1 FROM archived_workflow_step_retries WHERE id = p_id AND workflow_id = v_workflow_id AND namespace_id = v_namespace_id AND step_id = p_step_id AND retry_index = p_retry_index) THEN
        INSERT INTO archived_workflow_step_retries (id, workflow_id, namespace_id, step_id, retry_index, retry_type, time_stamp) 
        VALUES (p_id, v_workflow_id, v_namespace_id, p_step_id, p_retry_index, p_retry_type, p_time_stamp);
    END IF;
END;
$$;

ALTER TABLE "activities" 
ADD COLUMN IF NOT EXISTS "argument_definition" jsonb;

ALTER TABLE "workflows" 
ADD COLUMN IF NOT EXISTS "argument_definition" jsonb;

CREATE OR REPLACE PROCEDURE "set_activity_argument_definition" (
    IN p_namespace character varying ,
    IN p_activity character varying ,
    IN p_definition jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_activity_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM activities WHERE name = p_activity AND namespace_id = v_namespace_id) THEN
        v_activity_id := gen_random_uuid();
        INSERT INTO activities (id, namespace_id, name) VALUES (v_activity_id, v_namespace_id, p_activity);
    ELSE
        SELECT id INTO v_activity_id FROM activities WHERE name = p_activity AND namespace_id = v_namespace_id;
    END IF;
    UPDATE activities
    SET "argument_definition" = p_definition
    WHERE namespace_id = v_namespace_id
    AND id = v_activity_id;
END;
$$;

CREATE OR REPLACE PROCEDURE "set_workflow_argument_definition" (
    IN p_namespace character varying ,
    IN p_workflow character varying ,
    IN p_definition jsonb
)
LANGUAGE plpgsql 
AS $$
DECLARE
    v_namespace_id uuid;
    v_workflow_id uuid;
BEGIN
	IF NOT EXISTS (SELECT 1 FROM namespaces WHERE name = p_namespace) THEN
        v_namespace_id := gen_random_uuid();
		INSERT INTO namespaces (id, name) VALUES (v_namespace_id, p_namespace);
    ELSE
        SELECT id INTO v_namespace_id FROM namespaces WHERE name = p_namespace;
	END IF;
    IF NOT EXISTS (SELECT 1 FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id) THEN
        v_workflow_id := gen_random_uuid();
        INSERT INTO workflows (id, namespace_id, name) VALUES (v_workflow_id, v_namespace_id, p_workflow);
    ELSE
        SELECT id INTO v_workflow_id FROM workflows WHERE name = p_workflow AND namespace_id = v_namespace_id;
    END IF;
    UPDATE workflows
    SET "argument_definition" = p_definition
    WHERE namespace_id = v_namespace_id
    AND id = v_workflow_id;
END;
$$;

CREATE OR REPLACE FUNCTION public.get_archived_workflow_metada_json(pid uuid, pworkflow_id uuid, pnamespace_id uuid)
 RETURNS jsonb
 LANGUAGE sql
AS $$
    SELECT COALESCE(
        jsonb_object_agg(
            key_name,
            values
        ),
        '{}'::jsonb
    )
    FROM (
        SELECT
            key_name,
            jsonb_agg("value" ORDER BY value_index) AS values
        FROM archived_workflow_metadata_entry
        WHERE id = pid
          AND workflow_id = pworkflow_id
          AND namespace_id = pnamespace_id
        GROUP BY key_name
    ) metadata;
$$;

CREATE OR REPLACE FUNCTION get_archived_workflow_step_retries_json(pid uuid, pworkflow_id uuid, pnamespace_id uuid, pstep_id int)
 RETURNS jsonb
 LANGUAGE sql
AS $$
SELECT json_agg(
  json_build_object(
    'RetryType', "retry_type",
    'Timestamp', "time_stamp"
  )
  ORDER BY retry_index
)
FROM archived_workflow_step_retries
WHERE id = pid
AND workflow_id = pworkflow_id
AND namespace_id = pnamespace_id
AND step_id = pstep_id;
$$;

CREATE OR REPLACE FUNCTION get_archived_workflow_step_json(pid uuid, pworkflow_id uuid, pnamespace_id uuid)
 RETURNS jsonb
 LANGUAGE sql
AS $$
SELECT json_agg(
  json_build_object(
    'Type', aws.step_type,
    'Index', aws.step_index,
    'Name', a.name,
    'StartTime', aws.start_time,
    'EndTime', aws.end_time,
    'Retries', get_archived_workflow_step_retries_json(pid, pworkflow_id, pnamespace_id, aws.step_id),
    'Input', aws.input,
    'Status', aws.result_status,
    'ErrorMessage', aws.error_message,
    'Result', aws.result,
    'ChildWorfklowID', aws.child_workflow_instance_id,
    'ChildWorkflowName', cwf.name
  )
  ORDER BY step_id
)
FROM archived_workflow_steps aws
LEFT JOIN activities a 
  ON aws.activity_id = a.id 
  AND aws.namespace_id = a.namespace_id
LEFT JOIN workflows cwf
  ON aws.child_workflow_id = cwf.id
  AND aws.namespace_id = cwf.namespace_id
WHERE aws.id = pid
AND aws.workflow_id = pworkflow_id
AND aws.namespace_id = pnamespace_id;
$$;

CREATE OR REPLACE FUNCTION jsonb_extract_path_recursive(
    p_json jsonb,
    p_path text[]
)
RETURNS SETOF jsonb
LANGUAGE plpgsql
IMMUTABLE
AS $$
BEGIN
    IF cardinality(p_path) = 0 THEN
        RETURN NEXT p_json;
        RETURN;
    END IF;

    IF jsonb_typeof(p_json) = 'array' THEN
        RETURN QUERY
        SELECT extracted.value
        FROM jsonb_array_elements(p_json) AS item(value)
        CROSS JOIN LATERAL
            jsonb_extract_path_recursive(item.value, p_path)
            AS extracted(value);

    ELSIF jsonb_typeof(p_json) = 'object'
          AND p_json ? p_path[1] THEN

        RETURN QUERY
        SELECT extracted.value
        FROM jsonb_extract_path_recursive(
            p_json -> p_path[1],
            p_path[2:]
        ) AS extracted(value);
    END IF;
END;
$$;

CREATE OR REPLACE FUNCTION try_bigint(p_value text)
RETURNS bigint
LANGUAGE plpgsql
STABLE
PARALLEL SAFE
RETURNS NULL ON NULL INPUT
AS $$
BEGIN
    -- Fast non-throwing validation.
    IF NOT pg_input_is_valid(p_value, 'bigint'::regtype) THEN
        RETURN NULL;
    END IF;

    -- Actual conversion.
    RETURN p_value::bigint;

    -- Defensive fallback if conversion ever still fails.
EXCEPTION
    WHEN data_exception THEN
        RETURN NULL;
END;
$$;


CREATE OR REPLACE FUNCTION try_numeric(p_value text)
RETURNS numeric
LANGUAGE plpgsql
STABLE
PARALLEL SAFE
RETURNS NULL ON NULL INPUT
AS $$
BEGIN
    -- Fast non-throwing validation.
    IF NOT pg_input_is_valid(p_value, 'numeric'::regtype) THEN
        RETURN NULL;
    END IF;

    -- Actual conversion.
    RETURN p_value::numeric;

    -- Defensive fallback if conversion ever still fails.
EXCEPTION
    WHEN data_exception THEN
        RETURN NULL;
END;
$$;


CREATE OR REPLACE FUNCTION try_timestamptz(p_value text)
RETURNS timestamptz
LANGUAGE plpgsql
STABLE
PARALLEL SAFE
RETURNS NULL ON NULL INPUT
AS $$
BEGIN
    -- Fast non-throwing validation.
    IF NOT pg_input_is_valid(p_value, 'timestamptz'::regtype) THEN
        RETURN NULL;
    END IF;

    -- Actual conversion.
    RETURN p_value::timestamptz;

    -- Defensive fallback if conversion ever still fails.
EXCEPTION
    WHEN data_exception THEN
        RETURN NULL;
END;
$$;