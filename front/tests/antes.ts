import 'reflect-metadata';

// O scheduler do React usa MessageChannel quando existe, e a porta aberta impede o Jest de sair; com setTimeout, não.
Reflect.deleteProperty(globalThis, 'MessageChannel');
