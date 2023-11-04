# Session Management

This document describes the design of DebugTools' Session Management system

## Overview

DebugTools provides a number of commands that require various forms of session state. This state sometimes needs to exist in the local process running DebugTools, and at other times needs to exist in a remote HostApp.
When a session only exists in a remote HostApp, a session "handle" may be required to pass references to specific sessions across process boundaries

## Hierarchy

LocalDbgSessionProviderFactory
    LocalDbgSessionProvider<T>
        DbgSuperSession
            ProfilerSession
    LocalDbgSessionStore (passed to providers only)