"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.DaemonClient = void 0;
const axios_1 = __importDefault(require("axios"));
/**
 * Client for communicating with Nexus Daemon REST API
 */
class DaemonClient {
    constructor(baseUrl = 'http://localhost:5050') {
        this.baseUrl = baseUrl;
        this.client = axios_1.default.create({
            baseURL: baseUrl,
            timeout: 30000,
            headers: {
                'Content-Type': 'application/json'
            }
        });
    }
    /**
     * Compile context from a solution/workspace path
     */
    async compileContext(taskType, solutionPath, targets = []) {
        const request = {
            taskType,
            solutionId: '',
            solutionPath,
            targets
        };
        const response = await this.client.post('/context/compile', request);
        return response.data;
    }
    /**
     * Fetch code snippet from a specific file
     */
    async fetchCode(path, startLine, endLine) {
        const response = await this.client.get('/code/fetch', {
            params: { path, startLine, endLine }
        });
        return response.data;
    }
    /**
     * Check if daemon is reachable
     */
    async ping() {
        try {
            await this.client.get('/swagger/index.html', { timeout: 2000 });
            return true;
        }
        catch {
            return false;
        }
    }
    getBaseUrl() {
        return this.baseUrl;
    }
}
exports.DaemonClient = DaemonClient;
//# sourceMappingURL=daemonClient.js.map