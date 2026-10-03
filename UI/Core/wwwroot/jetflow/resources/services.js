export const GetConfig = async () => {
    let result = await fetch('/jetflow/config');
    if (result.ok)
        return await result.json();
    throw 'Unable to get config';
}

export const GetDashboardStream = (namespace) => new EventSource('/jetflow/dashboard'+(namespace===null || namespace===undefined ? '' : `?ns=${namespace}`));

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
