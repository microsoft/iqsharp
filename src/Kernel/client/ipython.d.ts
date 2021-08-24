// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// This file provides TypeScript type declarations for the window.IPython
// object used for extensibility in Jupyter Notebook.

export interface IPython {
    notebook: Notebook;
}

export interface Notebook {
    kernel: Kernel;
}

export type ShellCallbacks = {
    shell?: {
        reply?: (message: Message<any>) => void,
        payload?: {[payload_name: string]: (message: Message<any>) => void}
    },
    iopub?: {
        output?: (message: Message<any>) => void,
        clear_output?: (message: Message<any>) => void
    },
    input?: (message: Message<any>) => void,
    clear_on_done?: boolean,
    shell_done?: (message: Message<any>) => void,
    iopub_done?: (message: Message<any>) => void
}
export interface Events {
    on(event: string, callback: (any) => void);
}

export interface CommSession {
    send(data: any): string;
    on_msg(callback: (message: Message<{ comm_id: string, data: any }>) => void): void;
    on_close(callback: (message: any) => void): void;
    close(data?: any): void;
}

export interface MessageHeader {
    date: string;
    msg_id: string;
    msg_type: string;
    session: string;
    username: string;
    version: string;
}

export interface Message<TContents> {
    buffers: any[];
    channel: "iopub" | "shell";
    content: TContents;
    header: MessageHeader;
    metdata: any;
    msg_id: string;
    msg_type: string;
    parent_header: MessageHeader;
}

export type CommMessage<TData> = Message<{
    comm_id: string,
    data: TData,
    target_name: string
}>;

export interface CommManager {
    new_comm(target_name: string, data?: any): CommSession;
    register_target<TData>(target_name, callback: (comm: CommSession, msg: CommMessage<TData>) => void): void;
}

export interface Kernel {
    events: Events;

    is_connected: () => Boolean;
    execute(code: string, callbacks: ShellCallbacks | undefined, options: {silent?: boolean, user_expressions?: object, allow_stdin?: boolean} | undefined): string;
    register_iopub_handler(msg_type: string, callback: (message: Message<any>) => void);
    send_shell_message(msg_type: string, content: object, callbacks?: ShellCallbacks, metadata?: object, buffers?: Array<any>) : string;
    comm_manager: CommManager;
}
