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
        methos:'DELETE'
    });
    return result.ok;
};