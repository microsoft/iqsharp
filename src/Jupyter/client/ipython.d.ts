export declare var IPython: IPython;

interface IPython {
    notebook: Notebook;
}

interface Notebook {
    kernel: Kernel;
}

type Message = any; // TODO

type ShellCallbacks = {
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

interface Kernel {
    execute(code: string, callbacks: ShellCallbacks | undefined, options: {silent?: boolean, user_expressions?: object, allow_stdin?: boolean} | undefined): string;
    send_shell_message(msg_type: string, content: object, callbacks: ShellCallbacks | undefined, metadata: object | undefined, buffers: Array<any> | undefined) : string;
}
