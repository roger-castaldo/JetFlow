export const GetConfig = async () => {
    let result = await fetch('/jetflow/config');
    if (result.ok)
        return await result.json();
    throw 'Unable to get config';
}

export const GetDashboardStream = (namespace) => new EventSource('/jetflow/dashboard'+(namespace===null || namespace===undefined ? '' : `?ns=${namespace}`));

export const GetLatestActivityPerformance = async (namespace) => {
    let result = await fetch(`/jetflow/performance/activity/latest${namespace ? `?ns=${namespace}` : ''}`);
    if (result.ok)
        return await result.json();
    throw 'Unable to get latest activity performance';
};

export const GetActivityPerformance = async (namespace, activityName, startDate, endDate) => {
    let result = await fetch(`/jetflow/performance/activity?activityName=${activityName}${namespace ? `&ns=${namespace}` : ''}&startTime=${startDate.toISOString()}&endTime=${endDate.toISOString()}`);
    if (result.ok)
        return await result.json();
    throw 'Unable to get activity performance';
};

export const GetLatestWorkflowPerformance = async (namespace) => {
    let result = await fetch(`/jetflow/performance/workflow/latest${namespace ? `?ns=${namespace}` : ''}`);
    if (result.ok)
        return await result.json();
    throw 'Unable to get latest workflow performance';
};

export const GetWorkflowPerformance = async (namespace, workflowName, startDate, endDate) => {
    let result = await fetch(`/jetflow/performance/workflow?workflowName=${workflowName}${namespace ? `&ns=${namespace}` : ''}&startTime=${startDate.toISOString()}&endTime=${endDate.toISOString()}`);
    if (result.ok)
        return await result.json();
    throw 'Unable to get workflow performance';
};

export const GetNamespaces = async () => {
    let result = await fetch('/jetflow/namespaces');
    if (result.ok)
        return await result.json();
    throw 'Unable to list namespaces';
};
export const AddNamespace = async (namespace) => {
    let result = await fetch('/jetflow/namespaces',
        {
            method:'POST',
            body:JSON.stringify({ns:namespace})
        }
    );
    return result.ok;
};
export const DeleteNamespace = async (namespace) => {
    let result = await fetch(`/jetflow/namespaces/${namespace}`,{
        method:'DELETE'
    });
    return result.ok;
};

export const GetActiveFlows = async (namespace, workflowName, metadata, argument, offset, pageSize) => {
    let result = await fetch('/jetflow/query/activeflows',{
        method:'QUERY',
        body:JSON.stringify({
            workflowNamespace:namespace,
            workflowName:workflowName,
            metadata:metadata,
            argument:argument,
            offset:offset,
            pageSize:pageSize
        }),
        headers:{
            'Content-Type':'application/json'
        }
    });
    if (result.ok)
        return await result.json();
    throw 'Unable to list active flows';
};

export const GetArchivedFlows = async (namespace, workflowName, startedAt, completedAt, metadata, argument, offset, pageSize) => {
    let result = await fetch('/jetflow/query/archivedflows',{
        method:'QUERY',
        body:JSON.stringify({
            workflowNamespace:namespace,
            workflowName:workflowName,
            startedAt:startedAt,
            completedAt:completedAt,
            metadata:metadata,
            argument:argument,
            offset:offset,
            pageSize:pageSize
        }),
        headers:{
            'Content-Type':'application/json'
        }
    });
    if (result.ok)
        return await result.json();
    throw 'Unable to list archived flows';
};

export const GetScheduledFlows = async(namespace) => {
    let result = await fetch(`/jetflow/scheduledflows${namespace ? `?ns=${namespace}` : ''}`,{
        method:'GET',
    });
    if (result.ok)
        return await result.json();
    throw 'Unable to list scheduled flows';
}

export const GetWorkflowNames = async (namespace) => {
    let result = await fetch(`/jetflow/workflows${namespace ? `?ns=${namespace}` : ''}`,{
        method:'GET',
    });
    if (result.ok)
        return await result.json();
    throw 'Unable to list workflow names';
}

export const GetWorkflowDefinition = async (namespace, workflowName) => {
    let result = await fetch(`/jetflow/workflows/${workflowName}${namespace ? `?ns=${namespace}` : ''}`,{
        method:'GET',
    });
    if (result.ok)
        return await result.json();
    return null;
}
