const fs = require('fs')
const path = require('path')

const defaultLanguage = 'zh-CN'
const filePath = path.join(__dirname, `${defaultLanguage}.json`)

const readTranslateJson = () => {
    const text = fs.readFileSync(filePath, { encoding: 'utf-8' })
    return JSON.parse(text)
}

const template = (tokens) => {
    return `namespace PZPack.View.Service
{
    internal partial class Translate
    {
        #pragma warning disable CS8618
${tokens}
        #pragma warning restore CS8618
    }
}
`
}

const createTokens = (json) => {
    const keys = Object.keys(json)
    const tokens = []
    for (const k of keys) {
        const s = k.replace(/\s/g, '_')
        tokens.push(`            [TranslateBind("${k}")] public static string ${s} { get; set; }`)
    }
    return tokens.join('\r\n')
}

const exec = () => {
    const json = readTranslateJson()
    const tokens = createTokens(json)
    const text = template(tokens)

    const code = path.join(__dirname, `TranslateBindings.cs`)
    fs.writeFileSync(code, text, { encoding: 'utf-8' })
}

exec()