import {readdir,unlink,readFile,writeFile} from 'node:fs/promises';
import { resolve } from "path";
import { exec } from 'node:child_process';

function sleep(ms) {
    return new Promise((resolve) => setTimeout(resolve, ms));
}

const serverUrl = 'http://localhost:5000/';
const dockerImageName = 'wonderful_herschel'

console.log('Starting docker libretranslate image...');
exec(`docker run -d -p 5000:5000 --name ${dockerImageName} libretranslate/libretranslate`);
console.log('Waiting for image to fully start...');
await sleep(15*60*1000);

let resp = await fetch(`${serverUrl}languages`);
const json = await resp.json();
const languages = json.find(l=>l.code==='en').targets.filter(l=>l!=='en');

const extractPlaceholders = (text) => {
    const values = [];

    const replaced = text.replace(/\{([^}]+)\}/g, (match, value) => {
        const index = values.push(value) - 1;
        return `{${index}}`;
    });

    return {
        text: replaced,
        values,
        restore: (processedText) =>
            processedText.replace(/\{(\d+)\}/g, (match, index) =>
                values[Number(index)] !== undefined
                    ? `{${values[Number(index)]}}`
                    : match
            )
    };
}

const processValue = async (val,lang)=>{
    if (typeof val === 'string'){
        let count = 0;
        let converted = extractPlaceholders(val);
        let text = null;
        while (count<5 && text===null){
            try{
                console.log(`attempting to translate ${converted.text} to ${lang}`);
                const res = await fetch(`${serverUrl}translate`, {
                    method: "POST",
                    body: JSON.stringify({
                        q: converted.text,
                        source: "en",
                        target: lang
                    }),
                    headers: { "Content-Type": "application/json" }
                });
                const resp = await res.json();
                text = resp.translatedText;
                if (text!==undefined && text.indexOf(' ')>0 && val.indexOf(' ')===-1){
                    text = text.substring(0,text.indexOf(' '));
                }
                text = converted.restore(text);
            }catch(err){
                console.log(err);
                count++;
                text=null;
                console.log('restarting docker');
                exec(`docker restart ${dockerImageName}`);
                await sleep(10000);
                console.log('docker restarted');
            }
        }
        return text;
    }else{
        let result = {};
        let props = Object.keys(val);
        for(let x=0;x<props.length;x++){
            result[props[x]] = await processValue(val[props[x]],lang);
            if (result[props[x]]===null)
                return null;
        }
        if (Object.keys(result).length===0){
            return null;
        }
        return result;
    }
}

const deleteAllExcept = async (dirPath, fileToKeep) => {
  try {
    // Read directory entries with file type metadata
    const entries = await fs.readdir(dirPath, { withFileTypes: true });

    const deletePromises = entries
      .filter((entry) => entry.isFile() && entry.name !== fileToKeep)
      .map((entry) => fs.unlink(path.join(dirPath, entry.name)));

    await Promise.all(deletePromises);
    console.log(`Cleared ${dirPath}, kept ${fileToKeep}`);
  } catch (err) {
    console.error('Error deleting files:', err);
  }
};

await deleteAllExcept(resolve('./i18n/messages'), 'en.json');

const data = JSON.parse(await readFile(resolve('./i18n/messages/en.json')));
const props = Object.keys(data);

for(let x=0;x<languages.length;x++){
    const lang = languages[x];
    console.log(`Translating to ${lang}`);

    let olang = {};
    for(let y=0;y<props.length;y++){
        olang[props[y]] = await processValue(data[props[y]],lang);
        if (olang[props[y]]===null){
            delete olang[props[y]];
        }
        await sleep(500);
    }
    await writeFile(resolve(`./i18n/messages/${lang}.json`), JSON.stringify(olang));
}

await writeFile(resolve(`./i18n/definitions.js`),`import en from './messages/en.json';
${languages.map(l=>`import ${l.replaceAll('-','_')} from './messages/${l}.json';`).join('\n')}

export const messages = {
    'en':en,
    ${languages.map(l=>`'${l}':${l.replaceAll('-','_')}`).join(',\n')}
};

export const locales = ['en',${languages.map(l=>`'${l}'`).join(',')}]
`);

console.log('stopping docker libretranslate image...');
exec(`docker stop ${dockerImageName}`);

