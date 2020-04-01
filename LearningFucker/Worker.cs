﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using LearningFucker.Models;
using LearningFucker.Handler;

namespace LearningFucker
{
    public class Worker
    {
        public Worker()
        {
            timer = new Timer();
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Interval = Fucker.POLLING_TIME;

            Fucker = new Fucker();

            WorkList = new List<TaskForWork>();

            User = new User();
            TaskList = new List<Models.Task>();
        }

        private Timer timer;
        private bool studyProcessing = false;
        public AppInfo AppInfo { get; private set; }
        public Fucker Fucker { get; private set; }

        public List<LearningFucker.Models.Task> TaskList { get; set; }
        public UserStatistics UserStatistics { get; private set; }

        public User User { get; private set; }

        public Timer Timer { get => timer; }

        public List<Study> Studies { get; set; }

        private List<TaskForWork> WorkList { get; set; }

        public Action<Worker> TaskRefresed;

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            UserStatistics = await Fucker.GetMyTaskInfo();
            if (UserStatistics == null)
            {
                throw new Exception("获取用户任务完成信息时失败, 请重新打开程序重试!");
            }

            await RefreshTaskList();

            //var user = await Fucker.GetUserInfo();
            //if (user == null)
            //    throw new Exception("用户已失效, 请重新登录;");

            if (TaskRefresed != null)
                TaskRefresed(this);
        }

        public async Task<bool> Login(string username, string password)
        {
            var user = await Fucker.Login(username, password);
            if (user == null) return false;
            else
            {
                User.UserId = username;
                User.Password = password;
                User.Clone(user);
                return true;
            }
        }

        public async System.Threading.Tasks.Task Init()
        {

            AppInfo = await Fucker.GetAppInfo();
            if (AppInfo == null)
            {
                throw new Exception("获取App信息时失败, 请重新打开程序重试!");
            }

            await RefreshTaskList();

            UserStatistics = await Fucker.GetMyTaskInfo();
            if(UserStatistics == null)
            {
                throw new Exception("获取用户任务完成信息时失败, 请重新打开程序重试!");
            }

            if (TaskRefresed != null)
                TaskRefresed(this);

        }

        public async void Refresh()
        {
            await RefreshTaskList();

            UserStatistics = await Fucker.GetMyTaskInfo();
            if (UserStatistics == null)
            {
                throw new Exception("获取用户任务完成信息时失败, 请重新打开程序重试!");
            }

            if (TaskRefresed != null)
                TaskRefresed(this);

        }

        private async System.Threading.Tasks.Task RefreshTaskList()
        {
            var taskList = await Fucker.GetTaskList("0");
            if (taskList == null)
            {
                throw new Exception("获取任务信息时失败, 请重新打开程序重试!");
            }
            foreach (var item in taskList.List)
            {
                var task = this.TaskList.FirstOrDefault(s => s.TaskType == item.TaskType);
                if (task == null)
                {
                    TaskList.Add(item);
                }
                else
                    task.Clone(item);
            }
        }

        public void StartWork(List<int> tasks, bool parallel)
        {
            if (tasks == null || tasks.Count == 0)
                return;

            Timer.Start();

            LaunchWorkList(tasks);

            WorkList[0].Start(Fucker);
        }

        private void LaunchWorkList(List<int> tasks)
        {
            LearningFucker.Models.Task task;
            foreach (var item in tasks)
            {
                switch (item)
                {
                    case 14:
                        task = TaskList.FirstOrDefault(s => s.TaskType == item);
                        TaskForWork taskForWork = new TaskForWork(task, new StudyHandler());
                        taskForWork.Completed = new Action<TaskForWork>(WorkItemCompleted);
                        WorkList.Add(taskForWork);

                        taskForWork = new TaskForWork(task, new ExamHandler());
                        taskForWork.Completed = new Action<TaskForWork>(WorkItemCompleted);
                        WorkList.Add(taskForWork);

                        break;
                    case 2:
                        task = TaskList.FirstOrDefault(s => s.TaskType == item);
                        taskForWork = new TaskForWork(task, new ElectiveHandler());
                        taskForWork.Completed = new Action<TaskForWork>(WorkItemCompleted);
                        WorkList.Add(taskForWork);

                        taskForWork = new TaskForWork(task, new ExerciseHandler());
                        taskForWork.Completed = new Action<TaskForWork>(WorkItemCompleted);
                        WorkList.Add(taskForWork);
                        break;
                }
            }
        }

        private void WorkItemCompleted(TaskForWork workItem)
        {
            var index = WorkList.IndexOf(workItem);
            if (index == WorkList.Count - 1)
                return;
            else
            {
                index++;
                WorkList[index].Start(Fucker);
            }
        }

        public void StopWork()
        {

        }



        
    }
}
