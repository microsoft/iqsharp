export interface IPython {
    notebook: Notebook;
}

export interface Notebook {
    kernel: Kernel;
}

export type Message = any; // TODO

export type ShellCallbacks = {
    shell?: {
        reply?: (message: Message) => void,
        payload?: {[payload_name: string]: (message: Message) => void}
    },
    iopub?: {
        output?: (message: Message) => void,
        clear_output?: (message: Message) => void
    },
    input?: (message: Message) => void,
    clear_on_done?: boolean,
    shell_done?: (message: Message) => void,
    iopub_done?: (message: Message) => void
}

export interface Events {
    on(event: string, callback: (any) => void);
}

export interface Kernel {
    events: Events;

    is_connected: () => Boolean;
    execute(code: string, callbacks: ShellCallbacks | undefined, options: {silent?: boolean, user_expressions?: object, allow_stdin?: boolean} | undefined): string;
    register_iopub_handler(msg_type: string, callback: (message: Message) => void);
    send_shell_message(msg_type: string, content: object, callbacks?: ShellCallbacks, metadata?: object, buffers?: Array<any>) : string;
}
