# Feliz Template

This template gets you up and running with a simple web app using [Fable](http://fable.io/) and [Feliz](https://github.com/Zaid-Ajaj/Feliz).

## Requirements

- [dotnet SDK](https://www.microsoft.com/net/download/core) v8.0 or higher
- [node.js](https://nodejs.org) v20+ LTS

## Editor

To write and edit your code, you can use either VS Code + [Ionide](http://ionide.io/), Emacs with [fsharp-mode](https://github.com/fsharp/emacs-fsharp-mode), [Rider](https://www.jetbrains.com/rider/) or Visual Studio.

It is recommended to use VS Code, as you can also profit from automated formatting using [Fantomas](https://github.com/fsprojects/Fantomas) and inline autocomplete for tailwindcss!

## Development

### Setup

This needs to be done only once.

1. `dotnet tool restore` - to install the dotnet tools used in this template
2. `npm i` - to install the npm dependencies

### Scripts

#### Run

Then to start development mode with hot module reloading, run:

```bash
npm start
```

#### Build

To build the application and make ready for production:

```bash
npm run build
```

This command builds the application and puts the generated files into the `/dist` directory (can be overwritten in vite.config.js).

#### Test

To run the tests in watch mode:

```bash
npm test
```
